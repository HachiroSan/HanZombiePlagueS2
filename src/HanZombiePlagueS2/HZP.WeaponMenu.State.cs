namespace HanZombiePlagueS2;

public sealed class PlayerLoadoutLifeState
{
    public bool PrimarySelected { get; set; }
    public bool SecondarySelected { get; set; }
    public string PrimaryLoadoutId { get; set; } = string.Empty;
    public string SecondaryLoadoutId { get; set; } = string.Empty;
}

public sealed class SavedLoadoutPreference
{
    public bool RememberLoadout { get; set; }
    public string PrimaryLoadoutId { get; set; } = string.Empty;
    public string SecondaryLoadoutId { get; set; } = string.Empty;
}

public class HZPWeaponMenuState
{
    private readonly Dictionary<int, PlayerLoadoutLifeState> _lifeStates = [];
    private readonly Dictionary<ulong, SavedLoadoutPreference> _savedPreferences = [];

    public PlayerLoadoutLifeState GetLifeState(int playerId)
    {
        if (!_lifeStates.TryGetValue(playerId, out var state))
        {
            state = new PlayerLoadoutLifeState();
            _lifeStates[playerId] = state;
        }

        return state;
    }

    public void ResetLifeState(int playerId)
    {
        _lifeStates.Remove(playerId);
    }

    public void ResetAllLifeStates()
    {
        _lifeStates.Clear();
    }

    public SavedLoadoutPreference GetSavedPreference(ulong steamId)
    {
        if (steamId == 0)
        {
            return new SavedLoadoutPreference();
        }

        if (!_savedPreferences.TryGetValue(steamId, out var preference))
        {
            preference = new SavedLoadoutPreference();
            _savedPreferences[steamId] = preference;
        }

        return preference;
    }

    public void SetSavedPreference(ulong steamId, bool rememberLoadout, string? primaryLoadoutId, string? secondaryLoadoutId)
    {
        if (steamId == 0)
        {
            return;
        }

        _savedPreferences[steamId] = new SavedLoadoutPreference
        {
            RememberLoadout = rememberLoadout,
            PrimaryLoadoutId = primaryLoadoutId?.Trim() ?? string.Empty,
            SecondaryLoadoutId = secondaryLoadoutId?.Trim() ?? string.Empty
        };
    }
}
