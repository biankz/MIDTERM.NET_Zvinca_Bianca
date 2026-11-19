using RoomHub.Api.Common.Cqrs;
using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Http;

namespace RoomHub.Api.Features.Rooms.GetRoom;

public static class GetRoomEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle).WithName("GetRoom");
    }

    private static async Task<Results<Ok<GetRoomResponse>, ProblemHttpResult>> Handle(
        Guid id,
        IQueryHandler<GetRoomQuery, GetRoomResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRoomQuery(id), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();
        return TypedResults.Ok(result.Value);
    }
}
