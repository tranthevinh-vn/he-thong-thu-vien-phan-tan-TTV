using Library.Contracts.Options;
using RabbitMQ.Client;

namespace Library.Contracts.Messaging;

public static class RabbitMqTopology
{
    public static async Task DeclareAsync(IChannel channel, RabbitMqOptions options, CancellationToken ct = default)
    {
        await channel.ExchangeDeclareAsync(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(options.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            options.ReservationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: DeadLetterArgs(options, options.DeadLetterQueue),
            cancellationToken: ct);
        await channel.QueueBindAsync(options.ReservationQueue, options.Exchange, RoutingKeys.ReservationRequested, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            options.AuditQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: DeadLetterArgs(options, options.AuditDeadLetterQueue),
            cancellationToken: ct);
        await channel.QueueBindAsync(options.AuditQueue, options.Exchange, RoutingKeys.ReservationWildcard, cancellationToken: ct);

        await channel.QueueDeclareAsync(options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(options.DeadLetterQueue, options.DeadLetterExchange, options.DeadLetterQueue, cancellationToken: ct);

        await channel.QueueDeclareAsync(options.AuditDeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(options.AuditDeadLetterQueue, options.DeadLetterExchange, options.AuditDeadLetterQueue, cancellationToken: ct);
    }

    private static Dictionary<string, object?> DeadLetterArgs(RabbitMqOptions options, string routingKey) => new()
    {
        ["x-dead-letter-exchange"] = options.DeadLetterExchange,
        ["x-dead-letter-routing-key"] = routingKey
    };
}
