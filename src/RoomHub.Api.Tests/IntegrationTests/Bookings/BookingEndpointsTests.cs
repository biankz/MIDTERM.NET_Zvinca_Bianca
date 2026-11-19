using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Tests.IntegrationTests;

namespace RoomHub.Api.Tests.IntegrationTests.Bookings;

public sealed class BookingEndpointsTests
{
    private static readonly DateTime FutureStartUtc = new(2099, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public static TheoryData<CreateBookingRequest, string, string> InvalidBookingRequests => new()
    {
        { new CreateBookingRequest(null, FutureStartUtc, FutureStartUtc.AddHours(1), 2), "Booking.InvalidBookedBy", "BookedBy is required." },
        { new CreateBookingRequest("  ", FutureStartUtc, FutureStartUtc.AddHours(1), 2), "Booking.InvalidBookedBy", "BookedBy is required." },
        { new CreateBookingRequest("Alex", FutureStartUtc, FutureStartUtc, 2), "Booking.InvalidRange", "The end must be after the start." },
        { new CreateBookingRequest("Alex", FutureStartUtc, FutureStartUtc.AddHours(1), 0), "Booking.InvalidAttendees", "Attendees must be positive." },
        { new CreateBookingRequest("Alex", FutureStartUtc, FutureStartUtc.AddHours(1), 9), "Booking.AttendeesExceedCapacity", "The room does not have enough seats." },
        { new CreateBookingRequest("Alex", new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc), 2), "Booking.StartsInPast", "A booking cannot start in the past." },
        { new CreateBookingRequest("Alex", FutureStartUtc, FutureStartUtc.AddHours(4).AddTicks(1), 2), "Booking.TooLong", "A booking cannot last more than 4 hours." }
    };

    [Fact]
    public async Task Given_ValidBooking_When_Creating_Then_ReturnsCreatedAndPersistsBooking()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        var request = new CreateBookingRequest("Alex", FutureStartUtc, FutureStartUtc.AddHours(1), 2);

        using var response = await client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateBookingResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(room.Id, body.RoomId);
        Assert.Equal("Active", body.Status);
        Assert.Equal($"/api/bookings/{body.Id}", response.Headers.Location?.OriginalString);

        var bookings = await IntegrationTestData.ReadBookingsAsync(factory);
        var stored = Assert.Single(bookings);
        Assert.Equal(body.Id, stored.Id);
        Assert.Equal(room.Id, stored.RoomId);
        Assert.Equal("Alex", stored.BookedBy);
        Assert.Equal(FutureStartUtc, stored.StartUtc);
        Assert.Equal(FutureStartUtc.AddHours(1), stored.EndUtc);
        Assert.Equal(2, stored.Attendees);
        Assert.Equal(BookingStatus.Active, stored.Status);
    }

    [Theory]
    [MemberData(nameof(InvalidBookingRequests))]
    public async Task Given_InvalidBooking_When_Creating_Then_ReturnsBadRequestWithoutPersistence(
        CreateBookingRequest request,
        string errorCode,
        string errorMessage)
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);

        using var response = await client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Equal(errorCode, problem.Title);
        Assert.Equal(errorMessage, problem.Detail);
        Assert.Equal(errorCode, problem.Extensions["code"]?.ToString());
        Assert.Empty(await IntegrationTestData.ReadBookingsAsync(factory));
    }

    [Fact]
    public async Task Given_MissingRoomAndInvalidRequest_When_Creating_Then_ReturnsNotFoundBeforeValidation()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new CreateBookingRequest(null, FutureStartUtc, FutureStartUtc, 0);

        using var response = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/bookings", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal("Room.NotFound", problem.Title);
        Assert.Equal("The room does not exist.", problem.Detail);
        Assert.Empty(await IntegrationTestData.ReadBookingsAsync(factory));
    }

    [Fact]
    public async Task Given_OverlappingActiveBooking_When_Creating_Then_ReturnsConflictWithoutPersistence()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        var existing = await IntegrationTestData.SeedBookingAsync(
            factory, room, FutureStartUtc, FutureStartUtc.AddHours(1));
        var request = new CreateBookingRequest(
            "Taylor", FutureStartUtc.AddMinutes(30), FutureStartUtc.AddHours(2), 2);

        using var response = await client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal("Booking.RoomOccupied", problem.Title);
        Assert.Equal("The room is occupied in this interval.", problem.Detail);
        var bookings = await IntegrationTestData.ReadBookingsAsync(factory);
        var stored = Assert.Single(bookings);
        Assert.Equal(existing.Id, stored.Id);
    }

    [Fact]
    public async Task Given_BookingStartingWhenAnotherEnds_When_Creating_Then_AllowsAdjacentIntervals()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        await IntegrationTestData.SeedBookingAsync(
            factory, room, FutureStartUtc, FutureStartUtc.AddHours(1));
        var request = new CreateBookingRequest(
            "Taylor", FutureStartUtc.AddHours(1), FutureStartUtc.AddHours(2), 2);

        using var response = await client.PostAsJsonAsync($"/api/rooms/{room.Id}/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var bookings = await IntegrationTestData.ReadBookingsAsync(factory);
        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, booking =>
            booking.StartUtc == FutureStartUtc.AddHours(1) &&
            booking.EndUtc == FutureStartUtc.AddHours(2));
    }

    [Fact]
    public async Task Given_ExistingBooking_When_Getting_Then_ReturnsBookingAndLeavesStateUnchanged()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory, 10);
        var booking = await IntegrationTestData.SeedBookingAsync(
            factory, room, FutureStartUtc, FutureStartUtc.AddHours(1));

        using var response = await client.GetAsync($"/api/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetBookingResponse>();
        Assert.NotNull(body);
        Assert.Equal(booking.Id, body.Id);
        Assert.Equal(room.Id, body.RoomId);
        Assert.Equal("Test participant", body.BookedBy);
        Assert.Equal(FutureStartUtc, body.StartUtc);
        Assert.Equal(FutureStartUtc.AddHours(1), body.EndUtc);
        Assert.Equal(2, body.Attendees);
        Assert.Equal("Active", body.Status);
        var stored = Assert.Single(await IntegrationTestData.ReadBookingsAsync(factory));
        Assert.Equal(BookingStatus.Active, stored.Status);
    }

    [Fact]
    public async Task Given_MissingBooking_When_Getting_Then_ReturnsNotFound()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/bookings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal("Booking.NotFound", problem.Title);
        Assert.Equal("The booking does not exist.", problem.Detail);
        Assert.Empty(await IntegrationTestData.ReadBookingsAsync(factory));
    }

    [Fact]
    public async Task Given_ActiveBooking_When_Cancelling_Then_ReturnsNoContentAndFreesItsInterval()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        var booking = await IntegrationTestData.SeedBookingAsync(
            factory, room, FutureStartUtc, FutureStartUtc.AddHours(1));

        using var response = await client.PostAsync($"/api/bookings/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        var cancelled = Assert.Single(await IntegrationTestData.ReadBookingsAsync(factory));
        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/rooms/{room.Id}/bookings",
            new CreateBookingRequest("Taylor", FutureStartUtc, FutureStartUtc.AddHours(1), 2));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var bookings = await IntegrationTestData.ReadBookingsAsync(factory);
        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, item => item.Id == booking.Id && item.Status == BookingStatus.Cancelled);
        Assert.Contains(bookings, item => item.Id != booking.Id && item.Status == BookingStatus.Active);
    }

    [Fact]
    public async Task Given_CancelledBooking_When_CancellingAgain_Then_ReturnsConflictWithoutChangingState()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();
        var room = await IntegrationTestData.SeedRoomAsync(factory);
        var booking = await IntegrationTestData.SeedBookingAsync(
            factory, room, FutureStartUtc, FutureStartUtc.AddHours(1), cancelled: true);

        using var response = await client.PostAsync($"/api/bookings/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        Assert.Equal("Booking.AlreadyCancelled", problem.Title);
        Assert.Equal("The booking is already cancelled.", problem.Detail);
        var stored = Assert.Single(await IntegrationTestData.ReadBookingsAsync(factory));
        Assert.Equal(booking.Id, stored.Id);
        Assert.Equal(BookingStatus.Cancelled, stored.Status);
    }

    [Fact]
    public async Task Given_MissingBooking_When_Cancelling_Then_ReturnsNotFound()
    {
        using var factory = new RoomHubWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync($"/api/bookings/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal("Booking.NotFound", problem.Title);
        Assert.Equal("The booking does not exist.", problem.Detail);
        Assert.Empty(await IntegrationTestData.ReadBookingsAsync(factory));
    }
}
