using RoomHub.Api.Common.Cqrs;
using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Bookings.CancelBooking;

public static class CancelBookingEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/cancel", Handle).WithName("CancelBooking");
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        ICommandHandler<CancelBookingCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelBookingCommand(id), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();
        return TypedResults.NoContent();
    }
}
