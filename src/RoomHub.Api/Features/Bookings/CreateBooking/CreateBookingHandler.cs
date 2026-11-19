using RoomHub.Api.Common.Cqrs;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed class CreateBookingHandler(IRoomRepository rooms, IBookingRepository bookings, IUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<CreateBookingCommand, CreateBookingResult>
{
    public async Task<Result<CreateBookingResult>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var room = await rooms.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
            return RoomErrors.NotFound;

        var active = await bookings.GetActiveForRoomAsync(command.RoomId, cancellationToken);
        var result = Booking.Create(room, command.BookedBy, command.StartUtc, command.EndUtc,
            command.Attendees, time.GetUtcNow().UtcDateTime, active);
        if (result.IsFailure)
            return result.Error;

        bookings.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateBookingResult(result.Value.Id, result.Value.RoomId, result.Value.Status.ToString());
    }
}
