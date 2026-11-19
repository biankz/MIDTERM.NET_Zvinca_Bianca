using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Bookings.CancelBooking;

namespace RoomHub.Api.Features.Bookings;

public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingFeatures(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");
        GetBookingEndpoint.Map(group);
        CancelBookingEndpoint.Map(group);
        return app;
    }
}
