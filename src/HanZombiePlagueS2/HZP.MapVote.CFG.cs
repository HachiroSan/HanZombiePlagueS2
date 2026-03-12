namespace HanZombiePlagueS2;

public sealed class HZPMapVoteMapEntry
{
    public bool Enable { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public int MinPlayers { get; set; } = 0;
    public int MaxPlayers { get; set; } = 0;

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

        return Enable && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Id);
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
