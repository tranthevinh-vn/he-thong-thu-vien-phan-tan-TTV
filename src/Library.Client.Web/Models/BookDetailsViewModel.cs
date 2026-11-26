using Library.Contracts.Dtos;

namespace Library.Client.Web.Models;

public class BookDetailsViewModel
{
    public BookDetailDto Book { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();
}
