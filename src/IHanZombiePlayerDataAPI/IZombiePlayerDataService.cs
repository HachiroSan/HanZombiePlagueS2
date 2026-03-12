namespace HanZombiePlayerData.Contracts;

/// <summary>
/// Zombie player data service for persistence.
/// </summary>
public interface IZombiePlayerDataService : IDisposable
{
    Task TouchPlayerAsync(ulong steamId, string? lastKnownName, CancellationToken cancellationToken = default);

    Task<ZombiePlayerPreferenceRecord?> GetPlayerPreferenceAsync(
        ulong steamId,
        string preferenceKey,
        CancellationToken cancellationToken = default);

    Task SavePlayerPreferenceAsync(
        ulong steamId,
        string preferenceKey,
        string? preferenceValue,
        CancellationToken cancellationToken = default);

    Task<ZombiePlayerStatsRecord> GetPlayerStatsAsync(
        ulong steamId,
        CancellationToken cancellationToken = default);

    Task IncrementPlayerStatsAsync(
        ulong steamId,
        ZombiePlayerStatsDelta delta,
        CancellationToken cancellationToken = default);
}