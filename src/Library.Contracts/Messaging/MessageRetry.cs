using System.Text;
using Library.Contracts.Options;
using RabbitMQ.Client;

namespace Library.Contracts.Messaging;

public static class MessageRetry
{
    public const string HeaderName = "x-retry-count";

    public static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers is null || !properties.Headers.TryGetValue(HeaderName, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }

    public static async Task RepublishAsync(
        IChannel channel,
        RabbitMqOptions options,
        string routingKey,
        ReadOnlyMemory<byte> body,
        IReadOnlyBasicProperties original,
        int retryCount,
        CancellationToken ct = default)
    {
        var headers = original.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(original.Headers);
        headers[HeaderName] = retryCount;

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = original.ContentType ?? "application/json",
            ContentEncoding = original.ContentEncoding ?? "utf-8",
            CorrelationId = original.CorrelationId,
            MessageId = original.MessageId,
            Headers = headers
        };

        await channel.BasicPublishAsync(
            exchange: options.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}
