using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoomHub.Api.Tests.IntegrationTests;

public sealed class RoomHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;
    private readonly DateTimeOffset? _utcNow;

    public RoomHubWebApplicationFactory(DateTimeOffset? utcNow = null)
    {
        _utcNow = utcNow;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"RoomHubTests-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:RoomHub", _connectionString);
        if (_utcNow is { } utcNow)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(utcNow));
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAliveConnection.Dispose();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
