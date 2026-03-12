namespace HanZombiePlayerData.Contracts;

public sealed class ZombiePlayerStatsDelta
{
    public int Infections { get; set; }

    public int Deaths { get; set; }

    public int RoundsPlayed { get; set; }

    public int RoundsWon { get; set; }
}