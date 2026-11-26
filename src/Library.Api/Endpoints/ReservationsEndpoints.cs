using System.Text.Json;
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

            if (!await db.Books.AnyAsync(b => b.Id == request.BookId, ct))
            {
                return Results.BadRequest(new { error = "Sách không tồn tại." });
            }

            var correlationId = http.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }
            http.Response.Headers["X-Correlation-Id"] = correlationId;

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

            var body = new { id = reservation.Id, status = reservation.Status, createdAt = reservation.CreatedAt };
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

        return app;
    }
}
