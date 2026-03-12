namespace HanZombiePlagueS2;

public class HZPStoreState
{
    private readonly Dictionary<int, Dictionary<string, int>> _lifePurchases = [];
    private readonly Dictionary<int, Dictionary<string, int>> _roundPurchases = [];

    public int GetLifePurchaseCount(int playerId, string itemId)
    {
        return GetCount(_lifePurchases, playerId, itemId);
    }

    public int GetRoundPurchaseCount(int playerId, string itemId)
    {
        return GetCount(_roundPurchases, playerId, itemId);
    }

    public void IncrementPurchase(int playerId, string itemId)
    {
        Increment(_lifePurchases, playerId, itemId);
        Increment(_roundPurchases, playerId, itemId);
    }

    public void ResetLifeState(int playerId)
    {
        _lifePurchases.Remove(playerId);
    }

    public void ResetRoundState()
    {
        _roundPurchases.Clear();
    }

    private static int GetCount(Dictionary<int, Dictionary<string, int>> source, int playerId, string itemId)
    {
        if (!source.TryGetValue(playerId, out var counts))
        {
            return 0;
        }

        return counts.TryGetValue(itemId, out var count) ? count : 0;
    }

    private static void Increment(Dictionary<int, Dictionary<string, int>> source, int playerId, string itemId)
    {
        if (!source.TryGetValue(playerId, out var counts))
        {
            counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            source[playerId] = counts;
        }

        counts[itemId] = counts.TryGetValue(itemId, out var count) ? count + 1 : 1;
    }
}
