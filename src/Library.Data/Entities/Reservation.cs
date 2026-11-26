using Library.Contracts.Enums;

namespace Library.Data.Entities;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid? BookCopyId { get; set; }
    public BookCopy? BookCopy { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
