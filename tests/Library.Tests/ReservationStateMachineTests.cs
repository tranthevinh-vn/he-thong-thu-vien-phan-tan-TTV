using Library.Contracts.Domain;
using Library.Contracts.Enums;

namespace Library.Tests;

public class ReservationStateMachineTests
{
    [Theory]
    [InlineData(ReservationStatus.Pending, true)]
    [InlineData(ReservationStatus.Reserved, false)]
    [InlineData(ReservationStatus.Rejected, false)]
    [InlineData(ReservationStatus.Borrowed, false)]
    [InlineData(ReservationStatus.Returned, false)]
    public void CanProcess_OnlyPending(ReservationStatus status, bool expected)
    {
        Assert.Equal(expected, ReservationStateMachine.CanProcess(status));
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, false)]
    [InlineData(ReservationStatus.Reserved, true)]
    [InlineData(ReservationStatus.Rejected, false)]
    [InlineData(ReservationStatus.Borrowed, false)]
    [InlineData(ReservationStatus.Returned, false)]
    public void CanBorrow_OnlyReserved(ReservationStatus status, bool expected)
    {
        Assert.Equal(expected, ReservationStateMachine.CanBorrow(status));
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, false)]
    [InlineData(ReservationStatus.Reserved, false)]
    [InlineData(ReservationStatus.Rejected, false)]
    [InlineData(ReservationStatus.Borrowed, true)]
    [InlineData(ReservationStatus.Returned, false)]
    public void CanReturn_OnlyBorrowed(ReservationStatus status, bool expected)
    {
        Assert.Equal(expected, ReservationStateMachine.CanReturn(status));
    }
}
