namespace Library.Contracts.Dtos;

public class CreateReservationRequest
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
}
