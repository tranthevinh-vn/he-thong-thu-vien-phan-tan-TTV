using Library.Contracts.Enums;

namespace Library.Data.Entities;

public class BookCopy
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public BookCopyStatus Status { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
