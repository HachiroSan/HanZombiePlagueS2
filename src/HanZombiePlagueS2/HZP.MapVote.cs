using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPMapVoteService(
    ISwiftlyCore core,
    HZPHelpers helpers,
    HZPMapVoteState state,
    IOptionsMonitor<HZPMapVoteCFG> mapVoteCFG,
    ILogger<HZPMapVoteService> logger)
{
    private HZPMapVoteMenu? _menu;

    public HZPMapVoteState State => state;

    public void SetMenu(HZPMapVoteMenu menu)
    {
        _menu = menu;
    }

    public void ResetOnMapLoad(string currentMapId, string currentWorkshopId)
    {
        if (!string.IsNullOrWhiteSpace(currentMapId))
        {
            EnqueueRecentMap(currentMapId);
        }

        float currentTime = 0;
        try
        {
            currentTime = core.Engine.GlobalVars.CurrentTime;
        }
        catch
        {
            currentTime = 0;
        }

        state.ResetMapState(currentMapId, currentWorkshopId, currentTime);
    }

    public void OnRoundStart()
    {
        CheckAutomatedVote();
    }

    public void OnRoundEnd()
    {
        state.RoundsPlayed++;
        if (state.MapChangeScheduled && state.VoteCompleted)
        {
            ChangeMap();
            return;
        }

        CheckAutomatedVote();
    }

    public bool CanPlayerVote(IPlayer player)
    {
        if (player == null || !player.IsValid || player.IsFakeClient)
        {
            return false;
        }

        if (mapVoteCFG.CurrentValue.AllowSpectatorsToVote)
        {
            return true;
        }

        return player.Controller?.TeamNum != 1;
    }

    public string GetNextMapDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(state.NextMapName))
        {
            return state.NextMapName;
        }

        return string.Empty;
    }

    public int GetVotes(string mapName)
    {
        return state.Votes.TryGetValue(mapName, out var votes) ? votes : 0;
    }

    public int GetTimeRemaining()
    {
        if (!state.VoteActive)
        {
            return 0;
        }

        return (int)Math.Max(0, Math.Ceiling((state.VoteEndTimeUtc - DateTime.UtcNow).TotalSeconds));
    }

    public string TryVote(IPlayer player, string mapName)
    {
        if (!state.VoteActive)
        {
            return "MapVoteNotActive";
        }

        if (!CanPlayerVote(player))
        {
            return "MapVoteVoteDenied";
        }

        if (!state.Votes.ContainsKey(mapName))
        {
            return "MapVoteNotActive";
        }

        if (state.PlayerVotes.TryGetValue(player.PlayerID, out var previousVote))
        {
            if (string.Equals(previousVote, mapName, StringComparison.OrdinalIgnoreCase))
            {
                return "MapVoteAlreadyVoted";
            }

            state.Votes[previousVote] = Math.Max(0, state.Votes[previousVote] - 1);
        }

        state.PlayerVotes[player.PlayerID] = mapName;
        state.Votes[mapName] = state.Votes.TryGetValue(mapName, out var currentVotes) ? currentVotes + 1 : 1;
        core.PlayerManager.SendChat(helpers.T(player, "MapVotePlayerVoted", player.Name, mapName, state.Votes[mapName]));
        return "MapVoteVoteRegistered";
    }

    public string TryRtv(IPlayer player)
    {
        var cfg = mapVoteCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableRtv)
        {
            return "MapVoteRtvDisabled";
        }

        if (state.MapChangeScheduled)
        {
            return "MapVoteAlreadyScheduled";
        }

        if (state.VoteActive)
        {
            return CanPlayerVote(player) ? "MapVoteVoteOpenNow" : "MapVoteVoteDenied";
        }

        int voters = GetEligibleVoterCount();
        if (voters < cfg.RtvMinPlayers)
        {
            return "MapVoteRtvMinPlayers";
        }

        if (state.RoundsPlayed < cfg.RtvMinRounds)
        {
            return "MapVoteRtvMinRounds";
        }

        if (!state.RtvVoters.Add(player.PlayerID))
        {
            return "MapVoteRtvAlreadyVoted";
        }

        int needed = GetRequiredRtvVotes(voters, cfg.RtvVotePercentage);
        core.PlayerManager.SendChat(helpers.T(player, "MapVoteRtvProgress", player.Name, state.RtvVoters.Count, needed));
        if (state.RtvVoters.Count >= needed)
        {
            StartVote(isRtv: true, changeMapImmediately: cfg.RtvChangeMapImmediately);
            return "MapVoteRtvPassed";
        }

        return "MapVoteRtvRegistered";
    }

    public string TryUnRtv(IPlayer player)
    {
        if (state.RtvVoters.Remove(player.PlayerID))
        {
            return "MapVoteRtvRemoved";
        }

        return "MapVoteRtvNotFound";
    }

    public string TryNominate(IPlayer player, string query)
    {
        var cfg = mapVoteCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableNomination)
        {
            return "MapVoteNominateDisabled";
        }

        var map = FindMap(query);
        if (map == null)
        {
            return "MapVoteNominateNotFound";
        }

        int playerCount = core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);
        if (!map.IsValidForPlayerCount(playerCount))
        {
            return "MapVoteNominatePlayerCount";
        }

        if (IsCurrentMap(map, core.Engine.GlobalVars.MapName.ToString(), core.Engine.WorkshopId))
        {
            return "MapVoteNominateCurrentMap";
        }

        if (IsMapInCooldown(map))
        {
            return "MapVoteNominateCooldown";
        }

        state.Nominations[player.PlayerID] = map.Name;
        core.PlayerManager.SendChat(helpers.T(player, "MapVoteNominateSuccess", player.Name, map.Name));
        return "MapVoteNominateStored";
    }

    public HZPMapVoteMapEntry? FindMap(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return mapVoteCFG.CurrentValue.MapList.FirstOrDefault(map =>
            map.Enable &&
            (map.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || map.Id.Equals(query, StringComparison.OrdinalIgnoreCase)));
    }

    public void CheckAutomatedVote(bool force = false)
    {
        var cfg = mapVoteCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableEndOfMapVote || state.VoteActive || state.VoteCompleted || state.MapChangeScheduled)
        {
            return;
        }

        bool shouldStart = force || IsNearMapEnd(cfg);
        if (!shouldStart)
        {
            return;
        }

        StartVote();
    }

    private void StartVote(bool isRtv = false, bool changeMapImmediately = false)
    {
        var cfg = mapVoteCFG.CurrentValue;
        var maps = BuildVoteMapPool().ToList();
        if (maps.Count == 0)
        {
            logger.LogWarning("Map vote could not start because no eligible maps were found.");
            return;
        }

        state.ResetVote();
        state.VoteSessionId++;
        state.VoteActive = true;
        state.VoteCompleted = false;
        state.ChangeMapImmediately = changeMapImmediately;
        state.VoteEndTimeUtc = DateTime.UtcNow.AddSeconds(cfg.VoteDuration);
        state.MapsInVote.AddRange(maps);
        foreach (var map in maps)
        {
            state.Votes[map.Name] = 0;
        }

        core.PlayerManager.SendChat(core.Localizer[isRtv ? "MapVoteRtvStarted" : "MapVoteStarted", cfg.VoteDuration]);
        _menu?.OpenVoteMenuForEligiblePlayers(true);

        int sessionId = state.VoteSessionId;
        core.Scheduler.DelayBySeconds(cfg.VoteDuration, () => FinishVote(sessionId));
    }

    private IEnumerable<HZPMapVoteMapEntry> BuildVoteMapPool()
    {
        var cfg = mapVoteCFG.CurrentValue;
        int playerCount = core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);
        string currentMapId = core.Engine.GlobalVars.MapName.ToString();
        string workshopId = core.Engine.WorkshopId;

        var eligible = cfg.MapList
            .Where(map => map.IsValidForPlayerCount(playerCount))
            .Where(map => !IsCurrentMap(map, currentMapId, workshopId))
            .Where(map => !IsMapInCooldown(map))
            .ToList();

        var nominations = state.Nominations.Values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => cfg.MapList.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Where(m => m != null)
            .Cast<HZPMapVoteMapEntry>()
            .Where(map => map.IsValidForPlayerCount(playerCount))
            .Where(map => !IsCurrentMap(map, currentMapId, workshopId))
            .Where(map => !IsMapInCooldown(map))
            .ToList();

        var random = new Random();
        var selected = new List<HZPMapVoteMapEntry>();
        foreach (var nomination in nominations.OrderBy(_ => random.Next()))
        {
            if (selected.Any(x => string.Equals(x.Name, nomination.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            selected.Add(nomination);
            if (selected.Count >= Math.Max(1, cfg.MapsToShow))
            {
                return selected;
            }
        }

        foreach (var map in eligible.OrderBy(_ => random.Next()))
        {
            if (selected.Any(x => string.Equals(x.Name, map.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            selected.Add(map);
            if (selected.Count >= Math.Max(1, cfg.MapsToShow))
            {
                break;
            }
        }

        return selected;
    }

    private void FinishVote(int sessionId)
    {
        if (!state.VoteActive || state.VoteSessionId != sessionId)
        {
            return;
        }

        state.VoteActive = false;
        state.VoteCompleted = true;
        _menu?.CloseAllVoteMenus();

        var winner = state.MapsInVote
            .OrderByDescending(map => GetVotes(map.Name))
            .ThenBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (winner == null)
        {
            state.VoteCompleted = false;
            return;
        }

        state.NextMapName = winner.Name;
        state.NextMapId = winner.Id;
        state.MapChangeScheduled = true;
        core.PlayerManager.SendChat(core.Localizer["MapVoteWinner", winner.Name]);
        if (state.ChangeMapImmediately)
        {
            ChangeMap();
        }
    }

    public void ChangeMap()
    {
        if (state.MapChangeInProgress || !state.MapChangeScheduled || string.IsNullOrWhiteSpace(state.NextMapName))
        {
            return;
        }

        state.MapChangeInProgress = true;
        var cfg = mapVoteCFG.CurrentValue;
        core.PlayerManager.SendChat(core.Localizer["MapVoteChanging", state.NextMapName, cfg.ChangeMapDelay]);
        core.Scheduler.DelayBySeconds(cfg.ChangeMapDelay, () =>
        {
            string targetId = string.IsNullOrWhiteSpace(state.NextMapId) ? state.NextMapName : state.NextMapId;
            if (targetId.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) || long.TryParse(targetId, out _))
            {
                string workshopId = targetId.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) ? targetId[3..] : targetId;
                core.Engine.ExecuteCommandWithBuffer($"nextlevel {state.NextMapName}", _ => { });
                core.Engine.ExecuteCommandWithBuffer($"host_workshop_map {workshopId}", _ => { });
            }
            else
            {
                core.Engine.ExecuteCommandWithBuffer($"nextlevel {state.NextMapName}", _ => { });
                core.Engine.ExecuteCommandWithBuffer($"changelevel {targetId}", _ => { });
            }
        });
    }

    private bool IsNearMapEnd(HZPMapVoteCFG cfg)
    {
        var maxRoundsConVar = core.ConVar.Find<int>("mp_maxrounds");
        if (maxRoundsConVar is { Value: > 0 } && cfg.TriggerRoundsBeforeEnd >= 0)
        {
            int roundsRemaining = Math.Max(0, maxRoundsConVar.Value - state.RoundsPlayed);
            if (roundsRemaining <= cfg.TriggerRoundsBeforeEnd)
            {
                return true;
            }
        }

        var timeLimitConVar = core.ConVar.Find<float>("mp_timelimit");
        if (timeLimitConVar is { Value: > 0 } && cfg.TriggerSecondsBeforeEnd > 0)
        {
            float currentTime;
            try
            {
                currentTime = core.Engine.GlobalVars.CurrentTime;
            }
            catch
            {
                currentTime = 0;
            }

            float mapSeconds = timeLimitConVar.Value * 60f;
            float elapsed = Math.Max(0, currentTime - state.MapStartTime);
            float remaining = Math.Max(0, mapSeconds - elapsed);
            if (remaining <= cfg.TriggerSecondsBeforeEnd)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCurrentMap(HZPMapVoteMapEntry map, string currentMapId, string workshopId)
    {
        return string.Equals(map.Id, currentMapId, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(workshopId) && string.Equals(map.Id, workshopId, StringComparison.OrdinalIgnoreCase))
            || string.Equals(map.Name, currentMapId, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMapInCooldown(HZPMapVoteMapEntry map)
    {
        return ContainsRecentMap(map.Id) || ContainsRecentMap(map.Name);
    }

    private void EnqueueRecentMap(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId))
        {
            return;
        }

        var cfg = mapVoteCFG.CurrentValue;
        if (cfg.MapsInCooldown <= 0)
        {
            state.RecentMaps.Clear();
            return;
        }

        if (ContainsRecentMap(mapId))
        {
            return;
        }

        state.RecentMaps.Enqueue(mapId);
        while (state.RecentMaps.Count > cfg.MapsInCooldown)
        {
            state.RecentMaps.Dequeue();
        }
    }

    private bool ContainsRecentMap(string mapId)
    {
        return state.RecentMaps.Any(x => string.Equals(x, mapId, StringComparison.OrdinalIgnoreCase));
    }

    private int GetEligibleVoterCount()
    {
        return core.PlayerManager.GetAllPlayers().Count(p => p != null && p.IsValid && !p.IsFakeClient && CanPlayerVote(p));
    }

    private static int GetRequiredRtvVotes(int totalPlayers, int percentage)
    {
        if (totalPlayers <= 0)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Ceiling(totalPlayers * (percentage / 100f)));
    }
}
