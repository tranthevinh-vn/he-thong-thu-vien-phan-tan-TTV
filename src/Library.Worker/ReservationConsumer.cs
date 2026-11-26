using System.Text.Json;
using Library.Contracts.Domain;
using Library.Contracts.Enums;
using Library.Contracts.Events;
using Library.Contracts.Messaging;
using Library.Contracts.Options;
using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Library.Worker;

public sealed class ReservationConsumer : BackgroundService
{
    private const string ConsumerName = "reservation-worker";
    private const int MaxRetries = 3;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    public ReservationConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi consumer RabbitMQ, thử lại sau {Delay}s", ReconnectDelay.TotalSeconds);
                await CleanupAsync();
                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        await CleanupAsync();
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "library-worker",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true,
                consumerDispatchConcurrency: 4),
            ct);

        await RabbitMqTopology.DeclareAsync(_channel, _options, ct);
        await _channel.BasicQosAsync(0, 10, false, ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;
        await _channel.BasicConsumeAsync(_options.ReservationQueue, false, consumer, ct);

        _logger.LogInformation("Đang lắng nghe queue {Queue}", _options.ReservationQueue);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel;
        if (channel is null)
        {
            return;
        }

        var retryCount = MessageRetry.GetRetryCount(ea.BasicProperties);

        try
        {
            var evt = JsonSerializer.Deserialize<ReservationRequested>(ea.Body.Span, MessagingJson.Options)
                ?? throw new InvalidOperationException("Payload ReservationRequested không hợp lệ.");

            await ProcessAsync(evt, _stoppingToken);
            await channel.BasicAckAsync(ea.DeliveryTag, false, _stoppingToken);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Dừng xử lý message {MessageId}, message sẽ được giao lại", ea.BasicProperties.MessageId);
        }
        catch (Exception ex)
        {
            if (retryCount < MaxRetries)
            {
                _logger.LogWarning(ex, "Xử lý message {MessageId} lỗi (lần {Retry}), publish lại", ea.BasicProperties.MessageId, retryCount + 1);
                await MessageRetry.RepublishAsync(channel, _options, RoutingKeys.ReservationRequested, ea.Body, ea.BasicProperties, retryCount + 1, _stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, _stoppingToken);
            }
            else
            {
                _logger.LogError(ex, "Message {MessageId} lỗi sau {MaxRetries} lần, chuyển DLQ", ea.BasicProperties.MessageId, MaxRetries);
                await channel.BasicNackAsync(ea.DeliveryTag, false, false, _stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(ReservationRequested evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        if (await db.ProcessedMessages.AnyAsync(m => m.Consumer == ConsumerName && m.MessageId == evt.MessageId, ct))
        {
            _logger.LogInformation("Bỏ qua message trùng {MessageId}", evt.MessageId);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == evt.ReservationId, ct)
            ?? throw new InvalidOperationException($"Không tìm thấy reservation {evt.ReservationId}");

        var now = DateTime.UtcNow;

        if (!ReservationStateMachine.CanProcess(reservation.Status))
        {
            _logger.LogInformation("Reservation {ReservationId} đã xử lý (status={Status})", reservation.Id, reservation.Status);
            db.ProcessedMessages.Add(new ProcessedMessage { Consumer = ConsumerName, MessageId = evt.MessageId, ProcessedAt = now });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        var copyId = await FindAvailableCopyForUpdateAsync(db, evt.BookId, ct);

        if (copyId is null)
        {
            reservation.Status = ReservationStatus.Rejected;
            reservation.RejectReason = "Hết bản sách khả dụng";
            reservation.ProcessedAt = now;
            AddProcessedEvent(db, evt, ReservationStatus.Rejected, now);
        }
        else
        {
            var copy = await db.BookCopies.FirstAsync(c => c.Id == copyId.Value, ct);
            copy.Status = BookCopyStatus.Reserved;

            reservation.Status = ReservationStatus.Reserved;
            reservation.BookCopyId = copy.Id;
            reservation.ProcessedAt = now;
            AddProcessedEvent(db, evt, ReservationStatus.Reserved, now);
        }

        db.ProcessedMessages.Add(new ProcessedMessage { Consumer = ConsumerName, MessageId = evt.MessageId, ProcessedAt = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Đã xử lý reservation {ReservationId} -> {Status}", reservation.Id, reservation.Status);
    }

    private static async Task<Guid?> FindAvailableCopyForUpdateAsync(LibraryDbContext db, Guid bookId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = """
            SELECT "Id"
            FROM "BookCopies"
            WHERE "BookId" = @bookId AND "Status" = 'Available'
            ORDER BY "Id"
            LIMIT 1
            FOR UPDATE SKIP LOCKED
            """;

        var parameter = cmd.CreateParameter();
        parameter.ParameterName = "@bookId";
        parameter.Value = bookId;
        cmd.Parameters.Add(parameter);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is Guid id ? id : null;
    }

    private static void AddProcessedEvent(LibraryDbContext db, ReservationRequested evt, ReservationStatus status, DateTime now)
    {
        var processed = new ReservationProcessed
        {
            MessageId = Guid.NewGuid(),
            ReservationId = evt.ReservationId,
            Status = status.ToString(),
            CorrelationId = evt.CorrelationId,
            OccurredAt = now
        };

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = RoutingKeys.ReservationProcessed,
            Payload = JsonSerializer.Serialize(processed, MessagingJson.Options),
            OccurredAt = now,
            CorrelationId = evt.CorrelationId
        });
    }

    private async Task CleanupAsync()
    {
        if (_channel is not null)
        {
            try
            {
                if (_channel.IsOpen)
                {
                    await _channel.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi khi đóng channel");
            }

            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi khi đóng connection");
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
