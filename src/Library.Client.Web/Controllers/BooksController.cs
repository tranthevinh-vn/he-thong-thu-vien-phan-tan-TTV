using Library.Client.Web.Models;
using Library.Client.Web.Services;
using Library.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Library.Client.Web.Controllers;

public class BooksController : Controller
{
    private const int PageSize = 10;
    private readonly LibraryApiClient _api;

    public BooksController(LibraryApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _api.GetBooksAsync(q, page, PageSize, ct);
        ViewData["Query"] = q;

        return View(result ?? new PagedResult<BookDto> { Page = page, PageSize = PageSize });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
    {
        var book = await _api.GetBookAsync(id, ct);
        if (book is null)
        {
            return NotFound();
        }

        var users = await _api.GetUsersAsync(ct) ?? new List<UserDto>();

        return View(new BookDetailsViewModel { Book = book, Users = users });
    }
}
