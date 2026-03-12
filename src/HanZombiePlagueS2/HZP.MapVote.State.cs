namespace HanZombiePlagueS2;

public sealed class HZPMapVoteState
{
    public bool VoteActive { get; set; }
    public bool VoteCompleted { get; set; }
    public bool MapChangeScheduled { get; set; }
    public bool MapChangeInProgress { get; set; }
    public int VoteSessionId { get; set; }
    public int RoundsPlayed { get; set; }
    public float MapStartTime { get; set; }
    public string CurrentMapId { get; set; } = string.Empty;
    public string CurrentWorkshopId { get; set; } = string.Empty;
    public string NextMapName { get; set; } = string.Empty;
    public string NextMapId { get; set; } = string.Empty;
    public bool ChangeMapImmediately { get; set; }
    public DateTime VoteEndTimeUtc { get; set; }
    public Dictionary<string, int> Votes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, string> PlayerVotes { get; } = new();
    public List<HZPMapVoteMapEntry> MapsInVote { get; } = [];
    public Queue<string> RecentMaps { get; } = new();
    public HashSet<int> RtvVoters { get; } = [];
    public Dictionary<int, string> Nominations { get; } = new();

    public void ResetVote()
    {
        VoteActive = false;
        VoteCompleted = false;
        ChangeMapImmediately = false;
        VoteEndTimeUtc = DateTime.MinValue;
        Votes.Clear();
        PlayerVotes.Clear();
        MapsInVote.Clear();
        RtvVoters.Clear();
    }

    public void ResetMapState(string currentMapId, string currentWorkshopId, float currentTime)
    {
        ResetVote();
        MapChangeScheduled = false;
        MapChangeInProgress = false;
        RoundsPlayed = 0;
        CurrentMapId = currentMapId;
        CurrentWorkshopId = currentWorkshopId;
        NextMapName = string.Empty;
        NextMapId = string.Empty;
        MapStartTime = currentTime;
        Nominations.Clear();
    }
}
