using Library.Client.Web.Models;
using Library.Client.Web.Services;
using Library.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Library.Client.Web.Controllers;

public class ReservationsController : Controller
{
    private readonly LibraryApiClient _api;

    public ReservationsController(LibraryApiClient api) => _api = api;

    public async Task<IActionResult> Index(Guid? userId, string? status, CancellationToken ct = default)
    {
        var users = await _api.GetUsersAsync(ct) ?? new List<UserDto>();
        var selectedUserId = userId ?? users.FirstOrDefault()?.Id;

        var reservations = selectedUserId is null
            ? new List<ReservationDto>()
            : await _api.GetUserReservationsAsync(selectedUserId.Value, status, ct) ?? new List<ReservationDto>();

        return View(new ReservationsIndexViewModel
        {
            Users = users,
            SelectedUserId = selectedUserId,
            Status = status,
            Reservations = reservations
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        var created = await _api.CreateReservationAsync(userId, bookId, ct);
        if (created is null)
        {
            TempData["Error"] = "Không tạo được đơn đặt mượn.";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        TempData["Message"] = "Đã gửi yêu cầu đặt mượn. Đơn đang chờ worker xử lý.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
    {
        var reservation = await _api.GetReservationAsync(id, ct);
        if (reservation is null)
        {
            return NotFound();
        }

        return View(reservation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(Guid id, CancellationToken ct = default)
    {
        var result = await _api.BorrowAsync(id, ct);
        TempData[result is null ? "Error" : "Message"] = result is null
            ? "Không thể mượn sách. Kiểm tra trạng thái đơn."
            : "Đã ghi nhận mượn sách.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id, CancellationToken ct = default)
    {
        var result = await _api.ReturnAsync(id, ct);
        TempData[result is null ? "Error" : "Message"] = result is null
            ? "Không thể trả sách. Kiểm tra trạng thái đơn."
            : "Đã ghi nhận trả sách.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
