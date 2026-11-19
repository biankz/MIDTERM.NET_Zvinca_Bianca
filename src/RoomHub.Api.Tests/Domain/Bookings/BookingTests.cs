using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Tests.Domain.Bookings;

public sealed class BookingTests
{
    private static readonly DateTime NowUtc = new(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FutureStartUtc = NowUtc.AddDays(1);

    public static TheoryData<string?, DateTime, DateTime, int, string> InvalidBookingInputs => new()
    {
        { null, FutureStartUtc, FutureStartUtc.AddHours(1), 2, "Booking.InvalidBookedBy" },
        { "  ", FutureStartUtc, FutureStartUtc.AddHours(1), 2, "Booking.InvalidBookedBy" },
        { "Alex", FutureStartUtc, FutureStartUtc, 2, "Booking.InvalidRange" },
        { "Alex", FutureStartUtc.AddHours(1), FutureStartUtc, 2, "Booking.InvalidRange" },
        { "Alex", FutureStartUtc, FutureStartUtc.AddHours(1), 0, "Booking.InvalidAttendees" },
        { "Alex", FutureStartUtc, FutureStartUtc.AddHours(1), -1, "Booking.InvalidAttendees" },
        { "Alex", FutureStartUtc, FutureStartUtc.AddHours(1), 9, "Booking.AttendeesExceedCapacity" }
    };

    [Fact]
    public void Given_ValidBookingData_When_Creating_Then_ReturnsTrimmedActiveBooking()
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var result = Booking.Create(
            room, "  Alex  ", FutureStartUtc, FutureStartUtc.AddHours(1), 2, NowUtc, []);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(room.Id, result.Value.RoomId);
        Assert.Equal("Alex", result.Value.BookedBy);
        Assert.Equal(FutureStartUtc, result.Value.StartUtc);
        Assert.Equal(FutureStartUtc.AddHours(1), result.Value.EndUtc);
        Assert.Equal(2, result.Value.Attendees);
        Assert.Equal(BookingStatus.Active, result.Value.Status);
    }

    [Theory]
    [MemberData(nameof(InvalidBookingInputs))]
    public void Given_InvalidBookingData_When_Creating_Then_ReturnsValidationError(
        string? bookedBy,
        DateTime startUtc,
        DateTime endUtc,
        int attendees,
        string errorCode)
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var result = Booking.Create(
            room, bookedBy, startUtc, endUtc, attendees, NowUtc, []);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(errorCode, result.Error.Code);
    }

    [Fact]
    public void Given_StartInPast_When_Creating_Then_ReturnsValidationError()
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var result = Booking.Create(
            room,
            "Alex",
            NowUtc.AddTicks(-1),
            NowUtc.AddHours(1),
            2,
            NowUtc,
            []);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Booking.StartsInPast", result.Error.Code);
    }

    [Fact]
    public void Given_ExactlyFourHourDuration_When_Creating_Then_Succeeds()
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var result = Booking.Create(
            room,
            "Alex",
            FutureStartUtc,
            FutureStartUtc.AddHours(4),
            2,
            NowUtc,
            []);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.FromHours(4), result.Value.EndUtc - result.Value.StartUtc);
    }

    [Fact]
    public void Given_DurationLongerThanFourHours_When_Creating_Then_ReturnsValidationError()
    {
        // Arrange
        var room = CreateRoom();

        // Act
        var result = Booking.Create(
            room,
            "Alex",
            FutureStartUtc,
            FutureStartUtc.AddHours(4).AddTicks(1),
            2,
            NowUtc,
            []);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Booking.TooLong", result.Error.Code);
    }

    [Fact]
    public void Given_OverlappingActiveBooking_When_Creating_Then_ReturnsConflict()
    {
        // Arrange
        var room = CreateRoom();
        var existing = CreateExistingBooking(
            room, FutureStartUtc, FutureStartUtc.AddHours(1));

        // Act
        var result = Booking.Create(
            room,
            "Taylor",
            FutureStartUtc.AddMinutes(30),
            FutureStartUtc.AddHours(2),
            2,
            NowUtc,
            [existing]);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Booking.RoomOccupied", result.Error.Code);
    }

    [Fact]
    public void Given_AdjacentActiveBooking_When_Creating_Then_Succeeds()
    {
        // Arrange
        var room = CreateRoom();
        var existing = CreateExistingBooking(
            room, FutureStartUtc, FutureStartUtc.AddHours(1));

        // Act
        var result = Booking.Create(
            room,
            "Taylor",
            FutureStartUtc.AddHours(1),
            FutureStartUtc.AddHours(2),
            2,
            NowUtc,
            [existing]);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Given_OverlappingCancelledBooking_When_Creating_Then_Succeeds()
    {
        // Arrange
        var room = CreateRoom();
        var cancelled = CreateExistingBooking(
            room, FutureStartUtc, FutureStartUtc.AddHours(1));
        cancelled.Cancel();

        // Act
        var result = Booking.Create(
            room,
            "Taylor",
            FutureStartUtc,
            FutureStartUtc.AddHours(1),
            2,
            NowUtc,
            [cancelled]);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Given_ActiveBooking_When_Cancelling_Then_ChangesStatusToCancelled()
    {
        // Arrange
        var booking = CreateExistingBooking(
            CreateRoom(), FutureStartUtc, FutureStartUtc.AddHours(1));

        // Act
        var result = booking.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Given_CancelledBooking_When_CancellingAgain_Then_ReturnsConflictWithoutChangingState()
    {
        // Arrange
        var booking = CreateExistingBooking(
            CreateRoom(), FutureStartUtc, FutureStartUtc.AddHours(1));
        var firstCancellation = booking.Cancel();

        // Act
        var secondCancellation = booking.Cancel();

        // Assert
        Assert.True(firstCancellation.IsSuccess);
        Assert.True(secondCancellation.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondCancellation.Error.Type);
        Assert.Equal("Booking.AlreadyCancelled", secondCancellation.Error.Code);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    private static Room CreateRoom(int capacity = 8) =>
        Room.Create("Test room", capacity).Value;

    private static Booking CreateExistingBooking(Room room, DateTime startUtc, DateTime endUtc) =>
        Booking.Create(room, "Existing participant", startUtc, endUtc, 2, NowUtc, []).Value;
}
