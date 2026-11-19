using System.Net;
using System.Net.Http.Json;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;

namespace RoomHub.Api.Tests.IntegrationTests.ApiVersioning;

public sealed class VersionedApiEndpointsTests
{
    [Fact]
    public async Task Given_V1RoomRequest_When_CreatingAndGetting_Then_UsesVersionedRoutesAndLocation()
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new CreateRoomRequest("Versioned room", 6);

        // Act
        using var createResponse = await client.PostAsJsonAsync("/api/v1/rooms", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotNull(created);
        Assert.Equal($"/api/v1/rooms/{created.Id}", createResponse.Headers.Location?.OriginalString);

        // Act
        using var getResponse = await client.GetAsync($"/api/v1/rooms/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var room = await getResponse.Content.ReadFromJsonAsync<GetRoomResponse>();
        Assert.NotNull(room);
        Assert.Equal(created.Id, room.Id);
        Assert.Equal("Versioned room", room.Name);
        Assert.Equal(6, room.Capacity);

        var storedRooms = await IntegrationTestData.ReadRoomsAsync(factory);
        var storedRoom = Assert.Single(storedRooms);
        Assert.Equal(created.Id, storedRoom.Id);
    }

    [Fact]
    public async Task Given_V1BookingRequest_When_CreatingGettingAndCancelling_Then_UsesVersionedRoutesAndLocation()
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        var startUtc = new DateTime(2099, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var request = new CreateBookingRequest("Alex", startUtc, startUtc.AddHours(1), 2);

        // Act
        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/rooms/{room.Id}/bookings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateBookingResponse>();
        Assert.NotNull(created);
        Assert.Equal($"/api/v1/bookings/{created.Id}", createResponse.Headers.Location?.OriginalString);

        // Act
        using var getResponse = await client.GetAsync($"/api/v1/bookings/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var booking = await getResponse.Content.ReadFromJsonAsync<GetBookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(created.Id, booking.Id);
        Assert.Equal(room.Id, booking.RoomId);
        Assert.Equal("Active", booking.Status);

        // Act
        using var cancelResponse = await client.PostAsync(
            $"/api/v1/bookings/{created.Id}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);
        var storedBookings = await IntegrationTestData.ReadBookingsAsync(factory);
        var storedBooking = Assert.Single(storedBookings);
        Assert.Equal(created.Id, storedBooking.Id);
        Assert.Equal(RoomHub.Api.Domain.Bookings.BookingStatus.Cancelled, storedBooking.Status);
    }
}
