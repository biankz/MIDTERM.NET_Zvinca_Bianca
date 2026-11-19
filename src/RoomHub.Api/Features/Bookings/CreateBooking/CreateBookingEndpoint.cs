using RoomHub.Api.Common.Cqrs;
using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Http;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingRequest(string? BookedBy, DateTime StartUtc, DateTime EndUtc, int Attendees);
public sealed record CreateBookingResponse(Guid Id, Guid RoomId, string Status);

public static class CreateBookingEndpoint
{
    public static void Map(RouteGroupBuilder group, string apiPrefix, string routeNamePrefix)
    {
        group.MapPost("/{roomId:guid}/bookings",
            (Guid roomId,
                CreateBookingRequest request,
                ICommandHandler<CreateBookingCommand, CreateBookingResult> handler,
                CancellationToken cancellationToken) =>
                    Handle(roomId, request, handler, apiPrefix, cancellationToken))
            .WithName($"{routeNamePrefix}CreateBooking");
    }

    private static async Task<Results<Created<CreateBookingResponse>, ProblemHttpResult>> Handle(
        Guid roomId,
        CreateBookingRequest request,
        ICommandHandler<CreateBookingCommand, CreateBookingResult> handler,
        string apiPrefix,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateBookingCommand(roomId, request.BookedBy,
            request.StartUtc, request.EndUtc, request.Attendees), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        var booking = result.Value;
        var response = new CreateBookingResponse(booking.Id, booking.RoomId, booking.Status);
        return TypedResults.Created($"{apiPrefix}/bookings/{booking.Id}", response);
    }
}
