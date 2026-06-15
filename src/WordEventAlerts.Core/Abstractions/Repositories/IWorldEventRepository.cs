using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

public interface IWorldEventRepository
{
    Task SaveAsync(WorldEvent worldEvent, CancellationToken cancellationToken = default);

    Task<WorldEvent?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorldEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}
