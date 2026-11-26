using Library.Contracts.Dtos;
using Library.Contracts.Enums;
using Library.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Endpoints;

public static class BooksEndpoints
{
    public static IEndpointRouteBuilder MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books").WithTags("Books");

        group.MapGet("/", async (LibraryDbContext db, int page = 1, int pageSize = 10, string? q = null) =>
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 10;

            var query = db.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim()}%";
                query = query.Where(b =>
                    EF.Functions.ILike(b.Title, term) ||
                    EF.Functions.ILike(b.Author, term) ||
                    EF.Functions.ILike(b.Isbn, term) ||
                    EF.Functions.ILike(b.Category, term));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Isbn = b.Isbn,
                    Category = b.Category,
                    TotalCopies = b.Copies.Count,
                    AvailableCopies = b.Copies.Count(c => c.Status == BookCopyStatus.Available)
                })
                .ToListAsync();

            var result = new PagedResult<BookDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return Results.Ok(result);
        })
        .WithName("GetBooks");

        group.MapGet("/{id:guid}", async (Guid id, LibraryDbContext db) =>
        {
            var book = await db.Books
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new BookDetailDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Isbn = b.Isbn,
                    Category = b.Category,
                    TotalCopies = b.Copies.Count,
                    AvailableCopies = b.Copies.Count(c => c.Status == BookCopyStatus.Available),
                    Copies = b.Copies
                        .OrderBy(c => c.Id)
                        .Select(c => new BookCopyDto { Id = c.Id, Status = c.Status })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return book is null ? Results.NotFound() : Results.Ok(book);
        })
        .WithName("GetBookById");

        return app;
    }
}
