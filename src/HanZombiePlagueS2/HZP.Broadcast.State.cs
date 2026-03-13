namespace HanZombiePlagueS2;

public sealed class HZPWelcomePendingPlayer
{
    public int Slot { get; set; }
    public ulong SteamId { get; set; }
    public DateTime DueTimeUtc { get; set; }
}

public sealed class HZPBroadcastState
{
    public Dictionary<int, HZPWelcomePendingPlayer> PendingWelcomePlayers { get; } = new();
    public HashSet<int> WelcomedSlots { get; } = [];
    public int CurrentAdIndex { get; set; }

    public void ClearPlayer(int slot)
    {
        PendingWelcomePlayers.Remove(slot);
        WelcomedSlots.Remove(slot);
    }
}
