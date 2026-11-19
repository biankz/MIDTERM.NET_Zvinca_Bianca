using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace RoomHub.Api.Tests.IntegrationTests;

public sealed class RoomHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;

    public RoomHubWebApplicationFactory()
    {
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
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAliveConnection.Dispose();
    }
}
