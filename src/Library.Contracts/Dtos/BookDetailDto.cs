namespace Library.Contracts.Dtos;

public class BookDetailDto : BookDto
{
    public List<BookCopyDto> Copies { get; set; } = new();
}
