using RoomHub.Api.Common.Cqrs;
using Microsoft.AspNetCore.Http.HttpResults;
using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Rooms.CreateRoom;

public sealed record CreateRoomRequest(string? Name, int Capacity);
public sealed record CreateRoomResponse(Guid Id, string Name, int Capacity);

public static class CreateRoomEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle).WithName("CreateRoom");
    }

    private static async Task<Results<Created<CreateRoomResponse>, ProblemHttpResult>> Handle(
        CreateRoomRequest request,
        ICommandHandler<CreateRoomCommand, Room> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateRoomCommand(request.Name, request.Capacity), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToProblem();

        var room = result.Value;
        return TypedResults.Created($"/api/rooms/{room.Id}",
            new CreateRoomResponse(room.Id, room.Name, room.Capacity));
    }
}
