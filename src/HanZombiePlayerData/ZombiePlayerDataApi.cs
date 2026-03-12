using HanZombiePlayerData.Contracts;
using Microsoft.Extensions.Logging;

namespace HanZombiePlayerData.Provider;

public sealed class ZombiePlayerDataApi : IZombiePlayerDataService
{
    public const string SharedInterfaceKey = "HanZombie.Database.v1";

    private bool _disposed;
    private ILogger<ZombiePlayerDataApi>? _logger;
    private ZombiePlayerDataRepository? _repository;

    internal void Initialize(ILogger<ZombiePlayerDataApi> logger, ZombiePlayerDataRepository repository)
    {
        _logger = logger;
        _repository = repository;
        _disposed = false;
    }

    public Task TouchPlayerAsync(ulong steamId, string? lastKnownName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository!.TouchPlayerAsync(steamId, lastKnownName, cancellationToken);
    }

    public Task<ZombiePlayerPreferenceRecord?> GetPlayerPreferenceAsync(ulong steamId, string preferenceKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository!.GetPlayerPreferenceAsync(steamId, preferenceKey, cancellationToken);
    }

    public Task SavePlayerPreferenceAsync(ulong steamId, string preferenceKey, string? preferenceValue, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository!.SavePlayerPreferenceAsync(steamId, preferenceKey, preferenceValue, cancellationToken);
    }

    public Task<ZombiePlayerStatsRecord> GetPlayerStatsAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository!.GetPlayerStatsAsync(steamId, cancellationToken);
    }

    public Task IncrementPlayerStatsAsync(ulong steamId, ZombiePlayerStatsDelta delta, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _repository!.IncrementPlayerStatsAsync(steamId, delta, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger?.LogDebug("Disposing zombie player data shared API.");
        _repository = null;
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _repository == null)
        {
            throw new ObjectDisposedException(nameof(ZombiePlayerDataApi));
        }
    }
}