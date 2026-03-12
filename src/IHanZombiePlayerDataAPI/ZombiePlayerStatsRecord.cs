namespace HanZombiePlayerData.Contracts;

public sealed class ZombiePlayerStatsRecord
{
    public ulong SteamId { get; set; }

    public int Infections { get; set; }

    public int Deaths { get; set; }

    public int RoundsPlayed { get; set; }

    public int RoundsWon { get; set; }

    public DateTime UpdatedUtc { get; set; }
}