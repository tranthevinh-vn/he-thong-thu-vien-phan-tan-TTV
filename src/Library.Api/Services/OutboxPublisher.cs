using Library.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Services;

public sealed class OutboxPublisher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceScopeFactory scopeFactory, RabbitMqPublisher publisher, ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý outbox");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await _publisher.PublishAsync(message.Type, message.Payload, message.CorrelationId, ct);
                message.ProcessedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Đã publish outbox {OutboxId} type={Type} correlation={CorrelationId}",
                    message.Id, message.Type, message.CorrelationId);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                await db.SaveChangesAsync(ct);

                _logger.LogWarning(
                    ex,
                    "Publish outbox {OutboxId} thất bại (lần {RetryCount}), sẽ thử lại sau",
                    message.Id, message.RetryCount);

                break;
            }
        }
    }
}
