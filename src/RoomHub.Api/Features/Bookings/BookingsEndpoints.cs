using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Bookings.CancelBooking;

namespace RoomHub.Api.Features.Bookings;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingFeatures(
        this IEndpointRouteBuilder app,
        string apiPrefix = "/api",
        string routeNamePrefix = "")
    {
        var group = app.MapGroup($"{apiPrefix}/bookings").WithTags("Bookings");
        GetBookingEndpoint.Map(group, routeNamePrefix);
        CancelBookingEndpoint.Map(group, routeNamePrefix);
        return app;
    }
}
