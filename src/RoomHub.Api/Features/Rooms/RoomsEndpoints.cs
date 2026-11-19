using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;
using RoomHub.Api.Features.Bookings.CreateBooking;

namespace RoomHub.Api.Features.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomFeatures(
        this IEndpointRouteBuilder app,
        string apiPrefix = "/api",
        string routeNamePrefix = "")
    {
        var group = app.MapGroup($"{apiPrefix}/rooms").WithTags("Rooms");
        CreateRoomEndpoint.Map(group, apiPrefix, routeNamePrefix);
        GetRoomEndpoint.Map(group, routeNamePrefix);
        CreateBookingEndpoint.Map(group, apiPrefix, routeNamePrefix);
        return app;
    }
}
