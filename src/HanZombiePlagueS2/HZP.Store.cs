using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPStoreService(
    HZPHelpers helpers,
    HZPServices services,
    HZPGlobals globals,
    HZPGameMode gameMode,
    HZPEconomyService economyService,
    HZPStoreState storeState,
    IOptionsMonitor<HZPMainCFG> mainCFG,
    IOptionsMonitor<HZPStoreCFG> storeCFG,
    ILogger<HZPStoreService> logger)
{
    public IEnumerable<HZPStoreItemEntry> GetAvailableItemsForPlayer(IPlayer player)
    {
        return GetCatalogItemsForPlayer(player, item => item.ShowInStore);
    }

    public IEnumerable<HZPStoreItemEntry> GetAdminItemsForPlayer(IPlayer player)
    {
        return GetCatalogItemsForPlayer(player, item => item.ShowInAdminMenu);
    }

    private IEnumerable<HZPStoreItemEntry> GetCatalogItemsForPlayer(IPlayer player, Func<HZPStoreItemEntry, bool> predicate)
    {
        if (player == null || !player.IsValid)
        {
            return [];
        }

        return storeCFG.CurrentValue.ItemList
            .Where(item => item.Enable)
            .Where(predicate)
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .Where(item => IsModeAllowed(item.AllowedModes))
            .Where(item => IsItemVisibleToPlayer(player, item))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase);
    }

    public bool CanOpenStore(IPlayer player, out string denyKey)
    {
        denyKey = string.Empty;
        var cfg = storeCFG.CurrentValue;
        if (!cfg.Enable)
        {
            denyKey = "StoreDisabled";
            return false;
        }

        if (player == null || !player.IsValid)
        {
            return false;
        }

        if (cfg.AliveOnly && (player.Controller == null || !player.Controller.IsValid || !player.Controller.PawnIsAlive))
        {
            denyKey = "StoreAliveOnly";
            return false;
        }

        if (!globals.GameStart && !cfg.AllowDuringPrep)
        {
            denyKey = "StorePrepLocked";
            return false;
        }

        if (globals.GameStart && !cfg.AllowAfterGameStart)
        {
            denyKey = "StoreRoundLocked";
            return false;
        }

        return true;
    }

    public int GetBalance(IPlayer player)
    {
        return player == null || !player.IsValid ? 0 : economyService.GetBalance(player.SteamID);
    }

    public async Task<(bool Success, string MessageKey)> TryPurchaseAsync(IPlayer player, HZPStoreItemEntry item)
    {
        if (!CanOpenStore(player, out var denyKey))
        {
            return (false, denyKey);
        }

        if (!IsItemAllowed(player, item, out var itemDenyKey))
        {
            return (false, itemDenyKey);
        }

        string itemId = item.Id.Trim();
        if (item.MaxPerLife > 0 && storeState.GetLifePurchaseCount(player.PlayerID, itemId) >= item.MaxPerLife)
        {
            return (false, "StoreLifeLimitReached");
        }

        if (item.MaxPerRound > 0 && storeState.GetRoundPurchaseCount(player.PlayerID, itemId) >= item.MaxPerRound)
        {
            return (false, "StoreRoundLimitReached");
        }

        if (economyService.GetBalance(player.SteamID) < item.Price)
        {
            return (false, "StoreNotEnoughCash");
        }

        bool spent = await economyService.TrySpendCurrencyAsync(player.SteamID, item.Price, $"store:{itemId}");
        if (!spent)
        {
            return (false, "StoreNotEnoughCash");
        }

        if (!GrantItem(player, item))
        {
            await economyService.AddCurrencyAsync(player.SteamID, item.Price, $"store_refund:{itemId}");
            return (false, "StorePurchaseFailed");
        }

        storeState.IncrementPurchase(player.PlayerID, itemId);
        return (true, "StorePurchaseSuccess");
    }

    public (bool Success, string MessageKey) TryGrantAdminItem(IPlayer player, HZPStoreItemEntry item)
    {
        if (player == null || !player.IsValid)
        {
            return (false, "StorePurchaseFailed");
        }

        if (!item.ShowInAdminMenu)
        {
            return (false, "StorePurchaseFailed");
        }

        if (!IsItemAllowed(player, item, out var itemDenyKey))
        {
            return (false, itemDenyKey);
        }

        if (!GrantItem(player, item))
        {
            return (false, "StorePurchaseFailed");
        }

        return (true, "StorePurchaseSuccess");
    }

    private bool IsItemVisibleToPlayer(IPlayer player, HZPStoreItemEntry item)
    {
        return IsItemAllowed(player, item, out _);
    }

    private bool IsItemAllowed(IPlayer player, HZPStoreItemEntry item, out string denyKey)
    {
        denyKey = string.Empty;
        if (!IsModeAllowed(item.AllowedModes))
        {
            denyKey = "StoreItemModeLocked";
            return false;
        }

        globals.IsZombie.TryGetValue(player.PlayerID, out bool isZombie);
        if (item.HumanOnly && isZombie)
        {
            denyKey = "StoreHumanOnly";
            return false;
        }

        if (item.ZombieOnly && !isZombie)
        {
            denyKey = "StoreZombieOnly";
            return false;
        }

        if (item.DenySpecialHumans && !isZombie)
        {
            globals.IsHero.TryGetValue(player.PlayerID, out bool isHero);
            globals.IsSniper.TryGetValue(player.PlayerID, out bool isSniper);
            globals.IsSurvivor.TryGetValue(player.PlayerID, out bool isSurvivor);
            if (isHero || isSniper || isSurvivor)
            {
                denyKey = "StoreSpecialHumanLocked";
                return false;
            }
        }

        return true;
    }

    private bool GrantItem(IPlayer player, HZPStoreItemEntry item)
    {
        try
        {
            return item.GrantType switch
            {
                StoreGrantType.TVaccine => GrantTVaccine(player),
                StoreGrantType.TVirus => GrantTVirus(player),
                StoreGrantType.AddHealth => GrantAddHealth(player, item),
                StoreGrantType.GodMode => GrantGodMode(player, item),
                StoreGrantType.InfiniteAmmo => GrantInfiniteAmmo(player, item),
                StoreGrantType.ScbaSuit => GrantScbaSuit(player),
                StoreGrantType.FireGrenade => GrantFireGrenade(player),
                StoreGrantType.LightGrenade => GrantLightGrenade(player),
                StoreGrantType.FreezeGrenade => GrantFreezeGrenade(player),
                StoreGrantType.TeleportGrenade => GrantTeleportGrenade(player),
                StoreGrantType.IncGrenade => GrantIncGrenade(player),
                StoreGrantType.TVirusGrenade => GrantTVirusGrenade(player),
                StoreGrantType.CustomWeapon => GrantCustomWeapon(player, item),
                _ => false
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to grant store item {ItemId} to {PlayerName}.", item.Id, player.Name);
            return false;
        }
    }

    private bool GrantAddHealth(IPlayer player, HZPStoreItemEntry item)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        int maxHealth = mainCFG.CurrentValue.HumanMaxHealth;
        int value = item.IntValue > 0 ? item.IntValue : 200;
        if (pawn.Health >= maxHealth)
        {
            return false;
        }

        helpers.AddHealth(player, maxHealth, value, mainCFG.CurrentValue.AddHealthSound);
        return true;
    }

    private bool GrantTVaccine(IPlayer player)
    {
        globals.IsHero.TryGetValue(player.PlayerID, out bool isHero);
        globals.IsSniper.TryGetValue(player.PlayerID, out bool isSniper);
        globals.IsSurvivor.TryGetValue(player.PlayerID, out bool isSurvivor);

        int maxHealth = isHero ? mainCFG.CurrentValue.Hero.HeroHealth
            : isSniper ? mainCFG.CurrentValue.Sniper.SniperHealth
            : isSurvivor ? mainCFG.CurrentValue.Survivor.SurvivorHealth
            : mainCFG.CurrentValue.HumanMaxHealth;

        string defaultModel = "characters/models/ctm_st6/ctm_st6_variante.vmdl";
        string customModel = string.IsNullOrEmpty(mainCFG.CurrentValue.HumandefaultModel)
            ? defaultModel
            : mainCFG.CurrentValue.HumandefaultModel;

        helpers.TVaccine(player, maxHealth, mainCFG.CurrentValue.HumanInitialSpeed, customModel, mainCFG.CurrentValue.TVaccineSound, 1.0f);
        return true;
    }

    private bool GrantTVirus(IPlayer player)
    {
        services.SetPlayerZombie(player);
        return true;
    }

    private bool GrantGodMode(IPlayer player, HZPStoreItemEntry item)
    {
        float duration = item.FloatValue > 0 ? item.FloatValue : 20f;
        helpers.SetGodState(player, duration);
        return true;
    }

    private bool GrantInfiniteAmmo(IPlayer player, HZPStoreItemEntry item)
    {
        float duration = item.FloatValue > 0 ? item.FloatValue : 20f;
        helpers.SetInfiniteAmmoState(player, duration);
        return true;
    }

    private bool GrantScbaSuit(IPlayer player)
    {
        helpers.GiveScbaSuit(player, mainCFG.CurrentValue.ScbaSuitGetSound);
        return true;
    }

    private bool GrantFireGrenade(IPlayer player)
    {
        helpers.GiveFireGrenade(player);
        return true;
    }

    private bool GrantLightGrenade(IPlayer player)
    {
        helpers.GiveLightGrenade(player);
        return true;
    }

    private bool GrantFreezeGrenade(IPlayer player)
    {
        helpers.GiveFreezeGrenade(player);
        return true;
    }

    private bool GrantTeleportGrenade(IPlayer player)
    {
        helpers.GiveTeleprotGrenade(player);
        return true;
    }

    private bool GrantIncGrenade(IPlayer player)
    {
        helpers.GiveIncGrenade(player);
        return true;
    }

    private bool GrantTVirusGrenade(IPlayer player)
    {
        helpers.TVirusGrenade(player);
        return true;
    }

    private bool GrantCustomWeapon(IPlayer player, HZPStoreItemEntry item)
    {
        if (string.IsNullOrWhiteSpace(item.HiddenCommand))
        {
            return false;
        }

        player.ExecuteCommand(item.HiddenCommand);
        return true;
    }

    private bool IsModeAllowed(string allowedModes)
    {
        if (string.IsNullOrWhiteSpace(allowedModes))
        {
            return true;
        }

        foreach (var rawMode in allowedModes.Split(','))
        {
            var modeName = rawMode.Trim();
            if (modeName.Length == 0)
            {
                continue;
            }

            if (Enum.TryParse<GameModeType>(modeName, true, out var mode) && mode == gameMode.CurrentMode)
            {
                return true;
            }
        }

        return false;
    }
}
