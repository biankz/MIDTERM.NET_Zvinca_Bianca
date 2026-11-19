using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Domain.Bookings;

public enum BookingStatus
{
    Active,
    Cancelled
}

public sealed class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public string BookedBy { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int Attendees { get; private set; }
    public BookingStatus Status { get; private set; }

    public static Result<Booking> Create(Room room, string? bookedBy, DateTime startUtc, DateTime endUtc,
        int attendees, DateTime nowUtc, IReadOnlyList<Booking> activeBookings)
    {
        if (string.IsNullOrWhiteSpace(bookedBy))
            return BookingErrors.InvalidBookedBy;
        if (endUtc <= startUtc)
            return BookingErrors.InvalidRange;
        if (attendees <= 0)
            return BookingErrors.InvalidAttendees;
        if (attendees > room.Capacity)
            return BookingErrors.AttendeesExceedCapacity;

        var start = startUtc.Kind == DateTimeKind.Utc ? startUtc : startUtc.ToUniversalTime();
        var end = endUtc.Kind == DateTimeKind.Utc ? endUtc : endUtc.ToUniversalTime();
        if (start <= nowUtc)
            return BookingErrors.StartsInPast;
        if (end - start > TimeSpan.FromHours(4))
            return BookingErrors.TooLong;

        foreach (var existing in activeBookings)
        {
            if (existing.Status == BookingStatus.Active &&
                existing.StartUtc < end && start < existing.EndUtc)
                return BookingErrors.RoomOccupied;
        }

        return new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            BookedBy = bookedBy.Trim(),
            StartUtc = start,
            EndUtc = end,
            Attendees = attendees,
            Status = BookingStatus.Active
        };
    }

    public Result<Unit> Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            return BookingErrors.AlreadyCancelled;
        Status = BookingStatus.Cancelled;
        return Unit.Value;
    }
}

public static class BookingErrors
{
    public static readonly Error InvalidBookedBy = Error.Validation("Booking.InvalidBookedBy", "BookedBy is required.");
    public static readonly Error InvalidRange = Error.Validation("Booking.InvalidRange", "The end must be after the start.");
    public static readonly Error InvalidAttendees = Error.Validation("Booking.InvalidAttendees", "Attendees must be positive.");
    public static readonly Error AttendeesExceedCapacity = Error.Validation("Booking.AttendeesExceedCapacity", "The room does not have enough seats.");
    public static readonly Error StartsInPast = Error.Validation("Booking.StartsInPast", "A booking cannot start in the past.");
    public static readonly Error TooLong = Error.Validation("Booking.TooLong", "A booking cannot last more than 4 hours.");
    public static readonly Error RoomOccupied = Error.Conflict("Booking.RoomOccupied", "The room is occupied in this interval.");
    public static readonly Error AlreadyCancelled = Error.Conflict("Booking.AlreadyCancelled", "The booking is already cancelled.");
    public static readonly Error NotFound = Error.NotFound("Booking.NotFound", "The booking does not exist.");
}
