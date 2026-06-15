using System.Collections.Concurrent;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.InMemory.Repositories;

/// <summary>
/// In-memory implementation of world event persistence operations.
/// </summary>
public sealed class InMemoryWorldEventRepository : IWorldEventRepository
{
    private readonly ConcurrentDictionary<Guid, WorldEvent> _events = new();

    /// <inheritdoc />
    public Task SaveAsync(WorldEvent worldEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);

        cancellationToken.ThrowIfCancellationRequested();
        _events[worldEvent.EventId] = worldEvent;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorldEvent?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.TryGetValue(eventId, out var worldEvent);
        return Task.FromResult(worldEvent);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<WorldEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureValidPaging(skip, take);

        var result = _events.Values
            .OrderByDescending(e => e.IngestedAtUtc)
            .ThenByDescending(e => e.EventId)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<WorldEvent>>(result);
    }

    private static void EnsureValidPaging(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip cannot be negative.");
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }
    }
}
