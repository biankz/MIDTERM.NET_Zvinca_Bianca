using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Infrastructure.Persistence;

namespace RoomHub.Api.Infrastructure.Repositories;

public sealed class RoomRepository(RoomHubDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Rooms.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public void Add(Room room) => db.Rooms.Add(room);
}
