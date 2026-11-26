using System.Text;
using Library.Contracts.Messaging;
using Library.Contracts.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Library.Api.Services;

public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string routingKey, string payload, string correlationId, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);
        var body = Encoding.UTF8.GetBytes(payload);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid().ToString("N")
        };

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            if (_connection is null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    ClientProvidedName = "library-api",
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };

                _connection = await factory.CreateConnectionAsync(ct);
                _logger.LogInformation("Đã kết nối RabbitMQ {Host}:{Port}", _options.Host, _options.Port);
            }

            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                ct);
            await RabbitMqTopology.DeclareAsync(_channel, _options, ct);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            if (_channel.IsOpen)
            {
                await _channel.CloseAsync();
            }
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            if (_connection.IsOpen)
            {
                await _connection.CloseAsync();
            }
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
