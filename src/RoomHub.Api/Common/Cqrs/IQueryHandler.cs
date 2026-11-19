using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Common.Cqrs;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> Handle(TQuery query, CancellationToken cancellationToken);
}
