using System.Text.Json;
using Library.Contracts.Enums;
using Library.Contracts.Events;
using Library.Contracts.Messaging;

namespace Library.Tests;

public class MessagingJsonTests
{
    [Fact]
    public void ReservationRequested_RoundTrips()
    {
        var original = new ReservationRequested
        {
            MessageId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            CorrelationId = "correlation-1",
            OccurredAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(original, MessagingJson.Options);
        var copy = JsonSerializer.Deserialize<ReservationRequested>(json, MessagingJson.Options);

        Assert.NotNull(copy);
        Assert.Equal(original.MessageId, copy!.MessageId);
        Assert.Equal(original.ReservationId, copy.ReservationId);
        Assert.Equal(original.UserId, copy.UserId);
        Assert.Equal(original.BookId, copy.BookId);
        Assert.Equal(original.CorrelationId, copy.CorrelationId);
    }

    [Fact]
    public void Enum_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(ReservationStatus.Reserved, MessagingJson.Options);

        Assert.Equal("\"Reserved\"", json);
    }
}
