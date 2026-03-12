using HanZombiePlayerData.Contracts;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class ZombiePlayerDataBridge(
    ISwiftlyCore core,
    ILogger<ZombiePlayerDataBridge> logger,
    HZPGlobals globals,
    PlayerZombieState zombieState,
    HZPWeaponMenuState weaponMenuState)
{
    public const string SharedInterfaceKey = "HanZombie.Database.v1";

    private const string ZombieClassPreferenceKey = "zombie_class";
    private const string LoadoutRememberPreferenceKey = "loadout_remember";
    private const string LoadoutPrimaryPreferenceKey = "loadout_primary";
    private const string LoadoutSecondaryPreferenceKey = "loadout_secondary";

    private IZombiePlayerDataService? _dataService;
    private readonly HashSet<ulong> _roundParticipants = [];
    private bool _roundActive;
    private bool _roundOutcomeRecorded;

    public void SetService(IZombiePlayerDataService? dataService)
    {
        _dataService = dataService;
    }

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

        QueueStatsUpdate(attacker.SteamID, attacker.Name, new ZombiePlayerStatsDelta
        {
            Infections = 1
        });
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

        QueueStatsUpdate(player.SteamID, player.Name, new ZombiePlayerStatsDelta
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

            var delta = new ZombiePlayerStatsDelta
            {
                RoundsPlayed = 1,
                RoundsWon = humanWon == !player.IsZombie ? 1 : 0
            };

            QueueStatsUpdate(player.SteamId, player.Name, delta);
        }
    }

    private async Task LoadPlayerAsync(IPlayer player)
    {
        var dataService = _dataService;
        if (dataService == null)
        {
            return;
        }

        try
        {
            await dataService.TouchPlayerAsync(player.SteamID, player.Name);
            var preference = await dataService.GetPlayerPreferenceAsync(player.SteamID, ZombieClassPreferenceKey);
            var rememberLoadout = await dataService.GetPlayerPreferenceAsync(player.SteamID, LoadoutRememberPreferenceKey);
            var primaryLoadout = await dataService.GetPlayerPreferenceAsync(player.SteamID, LoadoutPrimaryPreferenceKey);
            var secondaryLoadout = await dataService.GetPlayerPreferenceAsync(player.SteamID, LoadoutSecondaryPreferenceKey);

            core.Scheduler.NextTick(() =>
            {
                zombieState.SetPlayerPreference(player.SteamID, preference?.PreferenceValue);
                weaponMenuState.SetSavedPreference(
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
        var dataService = _dataService;
        if (dataService == null)
        {
            return;
        }

        try
        {
            await dataService.SavePlayerPreferenceAsync(steamId, ZombieClassPreferenceKey, className);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save zombie preference for SteamID {SteamId}.", steamId);
        }
    }

    private async Task SaveLoadoutPreferenceAsync(ulong steamId, bool rememberLoadout, string? primaryLoadoutId, string? secondaryLoadoutId)
    {
        var dataService = _dataService;
        if (dataService == null)
        {
            return;
        }

        try
        {
            await dataService.SavePlayerPreferenceAsync(steamId, LoadoutRememberPreferenceKey, rememberLoadout ? "1" : "0");
            await dataService.SavePlayerPreferenceAsync(steamId, LoadoutPrimaryPreferenceKey, primaryLoadoutId);
            await dataService.SavePlayerPreferenceAsync(steamId, LoadoutSecondaryPreferenceKey, secondaryLoadoutId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save loadout preference for SteamID {SteamId}.", steamId);
        }
    }

    private void QueueStatsUpdate(ulong steamId, string? playerName, ZombiePlayerStatsDelta delta)
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

    private async Task SaveStatsAsync(ulong steamId, string? playerName, ZombiePlayerStatsDelta delta)
    {
        var dataService = _dataService;
        if (dataService == null)
        {
            return;
        }

        try
        {
            await dataService.TouchPlayerAsync(steamId, playerName);
            await dataService.IncrementPlayerStatsAsync(steamId, delta);
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
