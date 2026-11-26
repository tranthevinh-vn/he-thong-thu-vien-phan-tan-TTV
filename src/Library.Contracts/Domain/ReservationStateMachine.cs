using Library.Contracts.Enums;

namespace Library.Contracts.Domain;

public static class ReservationStateMachine
{
    public static bool CanProcess(ReservationStatus status) => status == ReservationStatus.Pending;

    public static bool CanBorrow(ReservationStatus status) => status == ReservationStatus.Reserved;

    public static bool CanReturn(ReservationStatus status) => status == ReservationStatus.Borrowed;
}
