using Library.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Library.Api.Health;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqPublisher _publisher;

    public RabbitMqHealthCheck(RabbitMqPublisher publisher) => _publisher = publisher;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _publisher.PingAsync(cancellationToken)
                ? HealthCheckResult.Healthy("RabbitMQ kết nối được.")
                : HealthCheckResult.Unhealthy("Không kết nối được RabbitMQ.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi kiểm tra RabbitMQ.", ex);
        }
    }
}
