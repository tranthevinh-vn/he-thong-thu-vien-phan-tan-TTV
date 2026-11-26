using System.Text.Json;
using Library.Contracts.Domain;
using Library.Contracts.Dtos;
using Library.Contracts.Enums;
using Library.Contracts.Events;
using Library.Contracts.Messaging;
using Library.Data;
using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Endpoints;

public static class ReservationsEndpoints
{
    public static IEndpointRouteBuilder MapReservationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reservations").WithTags("Reservations");

        group.MapPost("/", async (CreateReservationRequest request, LibraryDbContext db, HttpContext http, CancellationToken ct) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == request.UserId, ct))
            {
                return Results.BadRequest(new { error = "Người dùng không tồn tại." });
            }

            var bookTitle = await db.Books
                .Where(b => b.Id == request.BookId)
                .Select(b => b.Title)
                .FirstOrDefaultAsync(ct);

            if (bookTitle is null)
            {
                return Results.BadRequest(new { error = "Sách không tồn tại." });
            }

            var correlationId = ResolveCorrelationId(http);
            var now = DateTime.UtcNow;

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                BookId = request.BookId,
                Status = ReservationStatus.Pending,
                CreatedAt = now,
                CorrelationId = correlationId
            };

            var evt = new ReservationRequested
            {
                MessageId = Guid.NewGuid(),
                ReservationId = reservation.Id,
                UserId = request.UserId,
                BookId = request.BookId,
                CorrelationId = correlationId,
                OccurredAt = now
            };

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = RoutingKeys.ReservationRequested,
                Payload = JsonSerializer.Serialize(evt, MessagingJson.Options),
                OccurredAt = now,
                CorrelationId = correlationId
            };

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            db.Reservations.Add(reservation);
            db.OutboxMessages.Add(outbox);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var body = new ReservationDto
            {
                Id = reservation.Id,
                UserId = reservation.UserId,
                BookId = reservation.BookId,
                BookTitle = bookTitle,
                Status = reservation.Status,
                CreatedAt = reservation.CreatedAt
            };

            return Results.Accepted($"/reservations/{reservation.Id}", body);
        })
        .WithName("CreateReservation");

        group.MapGet("/{id:guid}", async (Guid id, LibraryDbContext db, CancellationToken ct) =>
        {
            var dto = await db.Reservations
                .AsNoTracking()
                .Where(r => r.Id == id)
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
                .FirstOrDefaultAsync(ct);

            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("GetReservationById");

        group.MapPost("/{id:guid}/borrow", async (Guid id, LibraryDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.Book)
                .Include(r => r.BookCopy)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (reservation is null)
            {
                return Results.NotFound();
            }

            if (!ReservationStateMachine.CanBorrow(reservation.Status))
            {
                return Results.Conflict(new { error = "Chỉ có thể mượn khi đơn ở trạng thái Reserved." });
            }

            if (reservation.BookCopy is null)
            {
                return Results.Conflict(new { error = "Đơn chưa được gán bản sách." });
            }

            reservation.Status = ReservationStatus.Borrowed;
            reservation.BookCopy.Status = BookCopyStatus.Borrowed;

            AddAuditLog(db, reservation, "reservation.borrowed", ResolveCorrelationId(http));
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDto(reservation));
        })
        .WithName("BorrowReservation");

        group.MapPost("/{id:guid}/return", async (Guid id, LibraryDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var reservation = await db.Reservations
                .Include(r => r.Book)
                .Include(r => r.BookCopy)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (reservation is null)
            {
                return Results.NotFound();
            }

            if (!ReservationStateMachine.CanReturn(reservation.Status))
            {
                return Results.Conflict(new { error = "Chỉ có thể trả khi đơn ở trạng thái Borrowed." });
            }

            if (reservation.BookCopy is null)
            {
                return Results.Conflict(new { error = "Đơn chưa được gán bản sách." });
            }

            reservation.Status = ReservationStatus.Returned;
            reservation.BookCopy.Status = BookCopyStatus.Available;

            AddAuditLog(db, reservation, "reservation.returned", ResolveCorrelationId(http));
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDto(reservation));
        })
        .WithName("ReturnReservation");

        return app;
    }

    private static ReservationDto ToDto(Reservation reservation) => new()
    {
        Id = reservation.Id,
        UserId = reservation.UserId,
        BookId = reservation.BookId,
        BookTitle = reservation.Book?.Title ?? string.Empty,
        Status = reservation.Status,
        CreatedAt = reservation.CreatedAt,
        ProcessedAt = reservation.ProcessedAt,
        RejectReason = reservation.RejectReason
    };

    private static void AddAuditLog(LibraryDbContext db, Reservation reservation, string eventName, string correlationId)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Event = eventName,
            Status = reservation.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId
        });
    }

    private static string ResolveCorrelationId(HttpContext http)
    {
        var correlationId = http.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        http.Response.Headers["X-Correlation-Id"] = correlationId;
        return correlationId;
    }
}
