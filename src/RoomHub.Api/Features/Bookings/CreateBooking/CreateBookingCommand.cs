using RoomHub.Api.Common.Cqrs;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingCommand(Guid RoomId, string? BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees)
    : ICommand<CreateBookingResult>;

public sealed record CreateBookingResult(Guid Id, Guid RoomId, string Status);
