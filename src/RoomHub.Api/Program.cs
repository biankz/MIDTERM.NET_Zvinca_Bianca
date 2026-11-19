using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Common.Cqrs;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings;
using RoomHub.Api.Features.Rooms;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.Api.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<RoomHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("RoomHub")));
builder.Services.AddScoped<IUnitOfWork>(provider =>
    provider.GetRequiredService<RoomHubDbContext>());
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddHandlers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/", () => "RoomHub API is running.");
app.MapRoomFeatures();
app.MapBookingFeatures();
app.MapRoomFeatures("/api/v1", "V1");
app.MapBookingFeatures("/api/v1", "V1");

app.Run();

public partial class Program;
