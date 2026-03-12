namespace HanZombiePlagueS2;

public sealed class HZPPlayerPreferenceRecord
{
    public ulong SteamId { get; set; }
    public string PreferenceKey { get; set; } = string.Empty;
    public string? PreferenceValue { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class HZPPlayerStatsDelta
{
    public int Infections { get; set; }
    public int Deaths { get; set; }
    public int RoundsPlayed { get; set; }
    public int RoundsWon { get; set; }
}

public sealed class HZPPlayerStatsRecord
{
    public ulong SteamId { get; set; }
    public int Infections { get; set; }
    public int Deaths { get; set; }
    public int RoundsPlayed { get; set; }
    public int RoundsWon { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class HZPPlayerCurrencyRecord
{
    public ulong SteamId { get; set; }
    public int Balance { get; set; }
    public int LifetimeEarned { get; set; }
    public int LifetimeSpent { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
