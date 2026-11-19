using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Infrastructure.Persistence;

namespace RoomHub.Api.Tests.IntegrationTests;

internal static class IntegrationTestData
{
    public static async Task<Room> SeedRoomAsync(
        RoomHubWebApplicationFactory factory,
        int capacity = 8)
    {
        var room = Room.Create("Test room", capacity).Value;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    public static async Task<Booking> SeedBookingAsync(
        RoomHubWebApplicationFactory factory,
        Room room,
        DateTime startUtc,
        DateTime endUtc,
        bool cancelled = false)
    {
        var booking = Booking.Create(
            room,
            "Test participant",
            startUtc,
            endUtc,
            2,
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Array.Empty<Booking>()).Value;
        if (cancelled)
            booking.Cancel();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    public static async Task<List<Room>> ReadRoomsAsync(RoomHubWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
        return await db.Rooms.AsNoTracking().ToListAsync();
    }

    public static async Task<List<Booking>> ReadBookingsAsync(RoomHubWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
        return await db.Bookings.AsNoTracking().ToListAsync();
    }
}
