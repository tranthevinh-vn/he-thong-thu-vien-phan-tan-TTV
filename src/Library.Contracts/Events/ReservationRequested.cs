namespace Library.Contracts.Events;

public class ReservationRequested
{
    public Guid MessageId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
