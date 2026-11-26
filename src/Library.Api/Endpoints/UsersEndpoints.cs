using Library.Contracts.Dtos;
using Library.Contracts.Enums;
using Library.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        group.MapGet("/", async (LibraryDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new UserDto { Id = u.Id, Name = u.Name, Email = u.Email })
                .ToListAsync(ct);

            return Results.Ok(users);
        })
        .WithName("GetUsers");

        group.MapGet("/{userId:guid}/reservations", async (Guid userId, string? status, LibraryDbContext db, CancellationToken ct) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            {
                return Results.NotFound();
            }

            var query = db.Reservations.AsNoTracking().Where(r => r.UserId == userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ReservationStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return Results.BadRequest(new { error = "Trạng thái không hợp lệ." });
                }

                query = query.Where(r => r.Status == parsedStatus);
            }

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    BookId = r.BookId,
                    BookTitle = r.Book.Title,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    RejectReason = r.RejectReason
                })
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithName("GetUserReservations");

        return app;
    }
}
