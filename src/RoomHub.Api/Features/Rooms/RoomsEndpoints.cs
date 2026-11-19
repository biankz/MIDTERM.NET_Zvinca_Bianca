using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;
using RoomHub.Api.Features.Bookings.CreateBooking;

namespace RoomHub.Api.Features.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomFeatures(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");
        CreateRoomEndpoint.Map(group);
        GetRoomEndpoint.Map(group);
        CreateBookingEndpoint.Map(group);
        return app;
    }
}
