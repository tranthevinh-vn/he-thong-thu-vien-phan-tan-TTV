using Library.Contracts.Enums;

namespace Library.Contracts.Dtos;

public class BookCopyDto
{
    public Guid Id { get; set; }
    public BookCopyStatus Status { get; set; }
}
