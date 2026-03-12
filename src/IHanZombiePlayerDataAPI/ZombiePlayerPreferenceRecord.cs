namespace HanZombiePlayerData.Contracts;

public sealed class ZombiePlayerPreferenceRecord
{
    public ulong SteamId { get; set; }

    public string PreferenceKey { get; set; } = string.Empty;

    public string? PreferenceValue { get; set; }

    public DateTime UpdatedUtc { get; set; }
}