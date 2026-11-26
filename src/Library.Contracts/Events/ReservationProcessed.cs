namespace Library.Contracts.Events;

public class ReservationProcessed
{
    public Guid MessageId { get; set; }
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
