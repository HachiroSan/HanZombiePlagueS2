namespace HanZombiePlagueS2;

public sealed class HZPMapVoteMapEntry
{
    public bool Enable { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string WorkshopMapId { get; set; } = string.Empty;
    public int MinPlayers { get; set; } = 0;
    public int MaxPlayers { get; set; } = 0;

    public bool IsWorkshopMap => TryGetWorkshopMapId(out _);

    public string ResolveMapName()
    {
        var resolvedName = NormalizeMapName(Name);
        return string.IsNullOrWhiteSpace(resolvedName) ? Id.Trim() : resolvedName;
    }

    public string ResolveTargetId()
    {
        if (TryGetWorkshopMapId(out var workshopMapId))
        {
            return workshopMapId;
        }

        var normalizedId = Id.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedId) && !IsLegacyWorkshopIdFormat(normalizedId))
        {
            return normalizedId;
        }

        return ResolveMapName();
    }

    public bool TryGetWorkshopMapId(out string workshopMapId)
    {
        workshopMapId = NormalizeWorkshopMapId(WorkshopMapId);
        if (!string.IsNullOrWhiteSpace(workshopMapId))
        {
            return true;
        }

        workshopMapId = TryParseNameWorkshopMapId(Name, out _, out var embeddedWorkshopMapId)
            ? embeddedWorkshopMapId
            : string.Empty;
        return !string.IsNullOrWhiteSpace(workshopMapId);
    }

    public static bool TryParseNameWorkshopMapId(string input, out string mapName, out string workshopMapId)
    {
        mapName = string.Empty;
        workshopMapId = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalizedInput = input.Trim();
        int separatorIndex = normalizedInput.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= normalizedInput.Length - 1)
        {
            return false;
        }

        mapName = normalizedInput[..separatorIndex].Trim();
        workshopMapId = NormalizeWorkshopMapId(normalizedInput[(separatorIndex + 1)..]);
        if (string.IsNullOrWhiteSpace(mapName) || string.IsNullOrWhiteSpace(workshopMapId))
        {
            mapName = string.Empty;
            workshopMapId = string.Empty;
            return false;
        }

        return true;
    }

    public bool IsValidForPlayerCount(int playerCount)
    {
        if (MinPlayers > 0 && playerCount < MinPlayers)
        {
            return false;
        }

        if (MaxPlayers > 0 && playerCount > MaxPlayers)
        {
            return false;
        }

        var mapName = ResolveMapName();
        if (!Enable || string.IsNullOrWhiteSpace(mapName))
        {
            return false;
        }

        if (IsWorkshopMap)
        {
            return TryGetWorkshopMapId(out _);
        }

        if (!string.IsNullOrWhiteSpace(Id))
        {
            return !IsLegacyWorkshopIdFormat(Id);
        }

        return true;
    }

    private static bool IsLegacyWorkshopIdFormat(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var normalized = id.Trim();
        return normalized.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) || long.TryParse(normalized, out _);
    }

    private static string NormalizeWorkshopMapId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("ws:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return long.TryParse(normalized, out _) ? normalized : string.Empty;
    }

    private static string NormalizeMapName(string value)
    {
        if (TryParseNameWorkshopMapId(value, out var mapName, out _))
        {
            return mapName;
        }

        return value.Trim();
    }
}

public sealed class HZPMapVoteCFG
{
    public bool Enable { get; set; } = true;
    public bool EnableEndOfMapVote { get; set; } = true;
    public string NextMapCommand { get; set; } = "sw_nextmap";
    public string RtvCommand { get; set; } = "sw_rtv";
    public string UnRtvCommand { get; set; } = "sw_unrtv";
    public string NominateCommand { get; set; } = "sw_nominate";
    public string RevoteCommand { get; set; } = "sw_revote";
    public bool EnableRtv { get; set; } = true;
    public bool EnableNomination { get; set; } = true;
    public int RtvMinPlayers { get; set; } = 2;
    public int RtvMinRounds { get; set; } = 1;
    public int RtvVotePercentage { get; set; } = 60;
    public bool RtvChangeMapImmediately { get; set; } = false;
    public int MapsToShow { get; set; } = 5;
    public int VoteDuration { get; set; } = 20;
    public int ChangeMapDelay { get; set; } = 6;
    public int TriggerSecondsBeforeEnd { get; set; } = 120;
    public int TriggerRoundsBeforeEnd { get; set; } = 2;
    public int MapsInCooldown { get; set; } = 3;
    public bool AllowSpectatorsToVote { get; set; } = false;
    public List<HZPMapVoteMapEntry> MapList { get; set; } = [];
}
