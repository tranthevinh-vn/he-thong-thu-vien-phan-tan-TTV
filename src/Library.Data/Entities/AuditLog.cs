namespace Library.Data.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
