using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;

namespace RoomHub.Api.Tests.IntegrationTests.Rooms;

public sealed class RoomEndpointsTests
{
    [Fact]
    public async Task Given_ValidRoom_When_Creating_Then_ReturnsCreatedAndPersistsRoom()
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new CreateRoomRequest("Room 101", 8);

        // Act
        using var response = await client.PostAsJsonAsync("/api/rooms", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotNull(body);
        Assert.Equal("Room 101", body.Name);
        Assert.Equal(8, body.Capacity);
        Assert.Equal($"/api/rooms/{body.Id}", response.Headers.Location?.OriginalString);

        var rooms = await IntegrationTestData.ReadRoomsAsync(factory);
        var stored = Assert.Single(rooms);
        Assert.Equal(body.Id, stored.Id);
        Assert.Equal(body.Name, stored.Name);
        Assert.Equal(body.Capacity, stored.Capacity);
    }

    [Theory]
    [InlineData(null, 8, "Room.InvalidName", "Room name is required.")]
    [InlineData("  ", 8, "Room.InvalidName", "Room name is required.")]
    [InlineData("Room 101", 0, "Room.InvalidCapacity", "Capacity must be positive.")]
    [InlineData("Room 101", -1, "Room.InvalidCapacity", "Capacity must be positive.")]
    public async Task Given_InvalidRoom_When_Creating_Then_ReturnsBadRequestWithoutPersistence(
        string? name,
        int capacity,
        string errorCode,
        string errorMessage)
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new CreateRoomRequest(name, capacity);

        // Act
        using var response = await client.PostAsJsonAsync("/api/rooms", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Equal(errorCode, problem.Title);
        Assert.Equal(errorMessage, problem.Detail);
        Assert.Equal(errorCode, problem.Extensions["code"]?.ToString());
        Assert.Empty(await IntegrationTestData.ReadRoomsAsync(factory));
    }

    [Fact]
    public async Task Given_ExistingRoom_When_Getting_Then_ReturnsRoomAndLeavesStateUnchanged()
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory, 12);

        // Act
        using var response = await client.GetAsync($"/api/rooms/{room.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetRoomResponse>();
        Assert.NotNull(body);
        Assert.Equal(room.Id, body.Id);
        Assert.Equal(room.Name, body.Name);
        Assert.Equal(room.Capacity, body.Capacity);

        var rooms = await IntegrationTestData.ReadRoomsAsync(factory);
        var stored = Assert.Single(rooms);
        Assert.Equal(room.Id, stored.Id);
        Assert.Equal(room.Capacity, stored.Capacity);
    }

    [Fact]
    public async Task Given_MissingRoom_When_Getting_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/rooms/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal("Room.NotFound", problem.Title);
        Assert.Equal("The room does not exist.", problem.Detail);
        Assert.Empty(await IntegrationTestData.ReadRoomsAsync(factory));
    }
}
