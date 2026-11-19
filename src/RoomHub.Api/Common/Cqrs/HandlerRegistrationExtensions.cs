using RoomHub.Api.Features.Bookings.CancelBooking;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Common.Cqrs;

public static class HandlerRegistrationExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateRoomCommand, Room>, CreateRoomHandler>();
        services.AddScoped<IQueryHandler<GetRoomQuery, GetRoomResponse>, GetRoomHandler>();
        services.AddScoped<ICommandHandler<CreateBookingCommand, CreateBookingResult>, CreateBookingHandler>();
        services.AddScoped<IQueryHandler<GetBookingQuery, GetBookingResponse>, GetBookingHandler>();
        services.AddScoped<ICommandHandler<CancelBookingCommand, Unit>, CancelBookingHandler>();
        return services;
    }
}
