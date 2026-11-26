using System.Text.Json;
using Library.Contracts.Events;
using Library.Contracts.Messaging;
using Library.Contracts.Options;
using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Library.Audit;

public sealed class AuditConsumer : BackgroundService
{
    private const string ConsumerName = "reservation-audit";
    private const int MaxRetries = 3;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    public AuditConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditConsumer> logger)
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
            ClientProvidedName = "library-audit",
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
        await _channel.BasicConsumeAsync(_options.AuditQueue, false, consumer, ct);

        _logger.LogInformation("Đang lắng nghe queue {Queue}", _options.AuditQueue);
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
            await RecordAsync(ea.RoutingKey, ea.Body, _stoppingToken);
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
                _logger.LogWarning(ex, "Ghi audit message {MessageId} lỗi (lần {Retry}), publish lại", ea.BasicProperties.MessageId, retryCount + 1);
                await MessageRetry.RepublishAsync(channel, _options, ea.RoutingKey, ea.Body, ea.BasicProperties, retryCount + 1, _stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, _stoppingToken);
            }
            else
            {
                _logger.LogError(ex, "Audit message {MessageId} lỗi sau {MaxRetries} lần, chuyển DLQ", ea.BasicProperties.MessageId, MaxRetries);
                await channel.BasicNackAsync(ea.DeliveryTag, false, false, _stoppingToken);
            }
        }
    }

    private async Task RecordAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        Guid messageId;
        Guid reservationId;
        string status;
        string correlationId;
        DateTime timestamp;

        switch (routingKey)
        {
            case RoutingKeys.ReservationRequested:
            {
                var evt = JsonSerializer.Deserialize<ReservationRequested>(body.Span, MessagingJson.Options)
                    ?? throw new InvalidOperationException("Payload ReservationRequested không hợp lệ.");
                messageId = evt.MessageId;
                reservationId = evt.ReservationId;
                status = "Requested";
                correlationId = evt.CorrelationId;
                timestamp = evt.OccurredAt;
                break;
            }
            case RoutingKeys.ReservationProcessed:
            {
                var evt = JsonSerializer.Deserialize<ReservationProcessed>(body.Span, MessagingJson.Options)
                    ?? throw new InvalidOperationException("Payload ReservationProcessed không hợp lệ.");
                messageId = evt.MessageId;
                reservationId = evt.ReservationId;
                status = evt.Status;
                correlationId = evt.CorrelationId;
                timestamp = evt.OccurredAt;
                break;
            }
            default:
                _logger.LogWarning("Bỏ qua routing key không hỗ trợ: {RoutingKey}", routingKey);
                return;
        }

        if (await db.ProcessedMessages.AnyAsync(m => m.Consumer == ConsumerName && m.MessageId == messageId, ct))
        {
            return;
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ReservationId = reservationId,
            Event = routingKey,
            Status = status,
            Timestamp = timestamp,
            CorrelationId = correlationId
        });
        db.ProcessedMessages.Add(new ProcessedMessage
        {
            Consumer = ConsumerName,
            MessageId = messageId,
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Audit {Event} reservation={ReservationId} status={Status} correlation={CorrelationId}",
            routingKey, reservationId, status, correlationId);
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
