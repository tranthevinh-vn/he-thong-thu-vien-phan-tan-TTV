namespace Library.Contracts.Messaging;

public static class RoutingKeys
{
    public const string ReservationRequested = "reservation.requested";
    public const string ReservationProcessed = "reservation.processed";
    public const string ReservationWildcard = "reservation.*";
}
