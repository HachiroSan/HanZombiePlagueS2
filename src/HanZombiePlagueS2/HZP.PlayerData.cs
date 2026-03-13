using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPPlayerDataService(
    ISwiftlyCore core,
    ILogger<HZPPlayerDataService> logger,
    HZPHelpers helpers,
    HZPGlobals globals,
    PlayerZombieState zombieState,
    HZPLoadoutState loadoutState,
    HZPDatabaseService databaseService,
    HZPEconomyService economyService,
    IOptionsMonitor<HZPEconomyCFG> economyCFG)
{
    private const string ZombieClassPreferenceKey = "zombie_class";
    private const string LoadoutRememberPreferenceKey = "loadout_remember";
    private const string LoadoutPrimaryPreferenceKey = "loadout_primary";
    private const string LoadoutSecondaryPreferenceKey = "loadout_secondary";

    private readonly HashSet<ulong> _roundParticipants = [];
    private bool _roundActive;
    private bool _roundOutcomeRecorded;

    public void LoadOnlinePlayers()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            LoadPlayer(player);
        }
    }

    public void LoadPlayer(IPlayer? player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return;
        }

        if (_roundActive)
        {
            _roundParticipants.Add(player.SteamID);
        }

        economyService.LoadPlayer(player);
        _ = LoadPlayerAsync(player);
    }

    public void SavePreference(ulong steamId, string? className)
    {
        if (steamId == 0)
        {
            return;
        }

        _ = SavePreferenceAsync(steamId, className);
    }

    public void SaveLoadoutPreference(ulong steamId, bool rememberLoadout, string? primaryLoadoutId, string? secondaryLoadoutId)
    {
        if (steamId == 0)
        {
            return;
        }

        _ = SaveLoadoutPreferenceAsync(steamId, rememberLoadout, primaryLoadoutId, secondaryLoadoutId);
    }

    public void HandleGameStartChanged(bool gameStart)
    {
        _roundActive = gameStart;

        if (!gameStart)
        {
            return;
        }

        _roundOutcomeRecorded = false;
        _roundParticipants.Clear();

        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (player == null || !player.IsValid || player.SteamID == 0)
            {
                continue;
            }

            _roundParticipants.Add(player.SteamID);
        }
    }

    public void RecordInfection(IPlayer? attacker, IPlayer? victim)
    {
        if (attacker == null || !attacker.IsValid || attacker.SteamID == 0)
        {
            return;
        }

        if (victim != null && victim.IsValid && victim.SteamID != 0 && _roundActive)
        {
            _roundParticipants.Add(victim.SteamID);
        }

        if (_roundActive)
        {
            _roundParticipants.Add(attacker.SteamID);
        }

        QueueStatsUpdate(attacker.SteamID, attacker.Name, new HZPPlayerStatsDelta
        {
            Infections = 1
        });

        _ = GrantInfectionRewardAsync(attacker.SteamID);
    }

    public void RecordDeath(IPlayer? player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return;
        }

        if (_roundActive)
        {
            _roundParticipants.Add(player.SteamID);
        }

        QueueStatsUpdate(player.SteamID, player.Name, new HZPPlayerStatsDelta
        {
            Deaths = 1
        });
    }

    public void RecordRoundOutcome(bool humanWon)
    {
        if (!_roundActive || _roundOutcomeRecorded)
        {
            return;
        }

        _roundOutcomeRecorded = true;
        _roundActive = false;

        var playerSnapshots = core.PlayerManager
            .GetAllPlayers()
            .Where(player => player != null && player.IsValid && player.SteamID != 0)
            .Select(player => new RoundPlayerSnapshot(
                player!.SteamID,
                player.Name,
                globals.IsZombie.TryGetValue(player.PlayerID, out var isZombie) && isZombie))
            .ToList();

        foreach (var player in playerSnapshots)
        {
            if (!_roundParticipants.Contains(player.SteamId))
            {
                continue;
            }

            var delta = new HZPPlayerStatsDelta
            {
                RoundsPlayed = 1,
                RoundsWon = humanWon == !player.IsZombie ? 1 : 0
            };

            QueueStatsUpdate(player.SteamId, player.Name, delta);

            _ = GrantRoundRewardsAsync(player, humanWon);
        }
    }

    private async Task GrantInfectionRewardAsync(ulong steamId)
    {
        int reward = economyCFG.CurrentValue.InfectionReward;
        if (reward <= 0)
        {
            return;
        }

        bool added = await economyService.AddCurrencyAsync(steamId, reward, "reward_infection");
        if (!added)
        {
            return;
        }

        string amount = helpers.FormatCurrency(reward);
        string balance = helpers.FormatCurrency(economyService.GetBalance(steamId));
        NotifyOnlinePlayer(steamId, player => helpers.SendChatT(player, "CashRewardInfection", amount, balance));
    }

    private async Task GrantRoundRewardsAsync(RoundPlayerSnapshot player, bool humanWon)
    {
        int participationReward = economyCFG.CurrentValue.ParticipationReward;
        int grantedParticipation = 0;
        if (participationReward > 0)
        {
            bool addedParticipation = await economyService.AddCurrencyAsync(player.SteamId, participationReward, "reward_participation");
            if (addedParticipation)
            {
                grantedParticipation = participationReward;
            }
        }

        int winReward = 0;
        if (humanWon && !player.IsZombie)
        {
            winReward = economyCFG.CurrentValue.HumanWinReward;
        }
        else if (!humanWon && player.IsZombie)
        {
            winReward = economyCFG.CurrentValue.ZombieWinReward;
        }

        int grantedWin = 0;
        if (winReward > 0)
        {
            bool addedWin = await economyService.AddCurrencyAsync(player.SteamId, winReward, humanWon ? "reward_human_win" : "reward_zombie_win");
            if (addedWin)
            {
                grantedWin = winReward;
            }
        }

        if (grantedParticipation <= 0 && grantedWin <= 0)
        {
            return;
        }

        string balance = helpers.FormatCurrency(economyService.GetBalance(player.SteamId));
        NotifyOnlinePlayer(player.SteamId, target =>
        {
            if (grantedParticipation > 0 && grantedWin > 0)
            {
                string key = humanWon ? "CashRewardRoundHumanWin" : "CashRewardRoundZombieWin";
                helpers.SendChatT(target, key, helpers.FormatCurrency(grantedParticipation), helpers.FormatCurrency(grantedWin), balance);
                return;
            }

            if (grantedWin > 0)
            {
                string key = humanWon ? "CashRewardHumanWinOnly" : "CashRewardZombieWinOnly";
                helpers.SendChatT(target, key, helpers.FormatCurrency(grantedWin), balance);
                return;
            }

            helpers.SendChatT(target, "CashRewardParticipationOnly", helpers.FormatCurrency(grantedParticipation), balance);
        });
    }

    private void NotifyOnlinePlayer(ulong steamId, Action<IPlayer> notify)
    {
        if (steamId == 0)
        {
            return;
        }

        core.Scheduler.NextTick(() =>
        {
            var player = core.PlayerManager
                .GetAllPlayers()
                .FirstOrDefault(p => p != null && p.IsValid && !p.IsFakeClient && p.SteamID == steamId);

            if (player == null)
            {
                return;
            }

            notify(player);
        });
    }

    private async Task LoadPlayerAsync(IPlayer player)
    {
        try
        {
            await databaseService.TouchPlayerAsync(player.SteamID, player.Name);
            var preference = await databaseService.GetPlayerPreferenceAsync(player.SteamID, ZombieClassPreferenceKey);
            var rememberLoadout = await databaseService.GetPlayerPreferenceAsync(player.SteamID, LoadoutRememberPreferenceKey);
            var primaryLoadout = await databaseService.GetPlayerPreferenceAsync(player.SteamID, LoadoutPrimaryPreferenceKey);
            var secondaryLoadout = await databaseService.GetPlayerPreferenceAsync(player.SteamID, LoadoutSecondaryPreferenceKey);

            core.Scheduler.NextTick(() =>
            {
                zombieState.SetPlayerPreference(player.SteamID, preference?.PreferenceValue);
                loadoutState.SetSavedPreference(
                    player.SteamID,
                    ParseRememberLoadout(rememberLoadout?.PreferenceValue),
                    primaryLoadout?.PreferenceValue,
                    secondaryLoadout?.PreferenceValue);
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load zombie player data for SteamID {SteamId}.", player.SteamID);
        }
    }

    private async Task SavePreferenceAsync(ulong steamId, string? className)
    {
        try
        {
            await databaseService.SavePlayerPreferenceAsync(steamId, ZombieClassPreferenceKey, className);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save zombie preference for SteamID {SteamId}.", steamId);
        }
    }

    private async Task SaveLoadoutPreferenceAsync(ulong steamId, bool rememberLoadout, string? primaryLoadoutId, string? secondaryLoadoutId)
    {
        try
        {
            await databaseService.SavePlayerPreferenceAsync(steamId, LoadoutRememberPreferenceKey, rememberLoadout ? "1" : "0");
            await databaseService.SavePlayerPreferenceAsync(steamId, LoadoutPrimaryPreferenceKey, primaryLoadoutId);
            await databaseService.SavePlayerPreferenceAsync(steamId, LoadoutSecondaryPreferenceKey, secondaryLoadoutId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save loadout preference for SteamID {SteamId}.", steamId);
        }
    }

    private void QueueStatsUpdate(ulong steamId, string? playerName, HZPPlayerStatsDelta delta)
    {
        if (steamId == 0)
        {
            return;
        }

        if (delta.Infections == 0 && delta.Deaths == 0 && delta.RoundsPlayed == 0 && delta.RoundsWon == 0)
        {
            return;
        }

        _ = SaveStatsAsync(steamId, playerName, delta);
    }

    private async Task SaveStatsAsync(ulong steamId, string? playerName, HZPPlayerStatsDelta delta)
    {
        try
        {
            await databaseService.TouchPlayerAsync(steamId, playerName);
            await databaseService.IncrementPlayerStatsAsync(steamId, delta);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update zombie stats for SteamID {SteamId}.", steamId);
        }
    }

    private sealed record RoundPlayerSnapshot(ulong SteamId, string? Name, bool IsZombie);

    private static bool ParseRememberLoadout(string? rawValue)
    {
        return string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawValue, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rawValue, "on", StringComparison.OrdinalIgnoreCase);
    }
}
