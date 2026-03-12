namespace HanZombiePlagueS2;

public class HZPEconomyState
{
    private readonly Dictionary<ulong, int> _balances = [];
    private readonly HashSet<ulong> _loadedPlayers = [];

    public int GetBalance(ulong steamId)
    {
        return _balances.TryGetValue(steamId, out var balance) ? balance : 0;
    }

    public bool IsLoaded(ulong steamId)
    {
        return steamId != 0 && _loadedPlayers.Contains(steamId);
    }

    public void SetBalance(ulong steamId, int balance)
    {
        if (steamId == 0)
        {
            return;
        }

        _balances[steamId] = Math.Max(0, balance);
        _loadedPlayers.Add(steamId);
    }

    public void AddBalance(ulong steamId, int delta)
    {
        SetBalance(steamId, GetBalance(steamId) + delta);
    }

    public void ClearBalance(ulong steamId)
    {
        if (steamId == 0)
        {
            return;
        }

        _balances.Remove(steamId);
        _loadedPlayers.Remove(steamId);
    }
}
