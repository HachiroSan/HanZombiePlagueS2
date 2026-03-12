namespace HanZombiePlagueS2;

public sealed class HZPDatabaseService(HZPDatabaseRepository repository)
{
    public Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        return repository.EnsureSchemaAsync(cancellationToken);
    }

    public Task TouchPlayerAsync(ulong steamId, string? lastKnownName, CancellationToken cancellationToken = default)
    {
        return repository.TouchPlayerAsync(steamId, lastKnownName, cancellationToken);
    }

    public Task<HZPPlayerPreferenceRecord?> GetPlayerPreferenceAsync(ulong steamId, string preferenceKey, CancellationToken cancellationToken = default)
    {
        return repository.GetPlayerPreferenceAsync(steamId, preferenceKey, cancellationToken);
    }

    public Task SavePlayerPreferenceAsync(ulong steamId, string preferenceKey, string? preferenceValue, CancellationToken cancellationToken = default)
    {
        return repository.SavePlayerPreferenceAsync(steamId, preferenceKey, preferenceValue, cancellationToken);
    }

    public Task<HZPPlayerStatsRecord> GetPlayerStatsAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        return repository.GetPlayerStatsAsync(steamId, cancellationToken);
    }

    public Task IncrementPlayerStatsAsync(ulong steamId, HZPPlayerStatsDelta delta, CancellationToken cancellationToken = default)
    {
        return repository.IncrementPlayerStatsAsync(steamId, delta, cancellationToken);
    }
}
