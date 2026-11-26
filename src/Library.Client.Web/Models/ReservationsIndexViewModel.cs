using Library.Contracts.Dtos;

namespace Library.Client.Web.Models;

public class ReservationsIndexViewModel
{
    public List<UserDto> Users { get; set; } = new();
    public Guid? SelectedUserId { get; set; }
    public string? Status { get; set; }
    public List<ReservationDto> Reservations { get; set; } = new();
}
