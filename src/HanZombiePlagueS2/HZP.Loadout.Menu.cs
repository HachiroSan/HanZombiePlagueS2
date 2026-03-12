using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

public enum LoadoutStage
{
    Primary,
    Secondary
}

public class HZPLoadoutMenu
{
    private static readonly HashSet<string> PrimaryWeaponNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_aug",
        "weapon_sg556",
        "weapon_famas",
        "weapon_galilar",
        "weapon_awp",
        "weapon_ssg08",
        "weapon_scar20",
        "weapon_g3sg1",
        "weapon_mp9",
        "weapon_mac10",
        "weapon_mp7",
        "weapon_mp5sd",
        "weapon_p90",
        "weapon_ump45",
        "weapon_bizon",
        "weapon_mag7",
        "weapon_nova",
        "weapon_sawedoff",
        "weapon_xm1014",
        "weapon_negev",
        "weapon_m249"
    };

    private static readonly HashSet<string> SecondaryWeaponNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_elite",
        "weapon_fiveseven",
        "weapon_glock",
        "weapon_hkp2000",
        "weapon_p250",
        "weapon_tec9",
        "weapon_cz75a",
        "weapon_deagle",
        "weapon_revolver",
        "weapon_usp_silencer"
    };

    private readonly ILogger<HZPLoadoutMenu> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPMenuHelper _menuHelper;
    private readonly HZPHelpers _helpers;
    private readonly HZPGlobals _globals;
    private readonly HZPGameMode _gameMode;
    private readonly HZPLoadoutState _state;
    private readonly HZPPlayerDataService _playerDataService;
    private readonly IOptionsMonitor<HZPLoadoutCFG> _loadoutCFG;

    public HZPLoadoutMenu(
        ISwiftlyCore core,
        ILogger<HZPLoadoutMenu> logger,
        HZPMenuHelper menuHelper,
        HZPHelpers helpers,
        HZPGlobals globals,
        HZPGameMode gameMode,
        HZPLoadoutState state,
        HZPPlayerDataService playerDataService,
        IOptionsMonitor<HZPLoadoutCFG> loadoutCFG)
    {
        _core = core;
        _logger = logger;
        _menuHelper = menuHelper;
        _helpers = helpers;
        _globals = globals;
        _gameMode = gameMode;
        _state = state;
        _playerDataService = playerDataService;
        _loadoutCFG = loadoutCFG;
    }

    public IMenuAPI? OpenLoadoutMenu(IPlayer player)
    {
        if (!CanUseLoadout(player, out var denyKey))
        {
            if (!string.IsNullOrWhiteSpace(denyKey) && player != null && player.IsValid)
            {
                _helpers.SendChatT(player, denyKey);
            }

            return null;
        }

        var lifeState = _state.GetLifeState(player.PlayerID);
        var stage = !lifeState.PrimarySelected || lifeState.SecondarySelected ? LoadoutStage.Primary : LoadoutStage.Secondary;
        return OpenStageMenu(player, stage);
    }

    public bool TryHandleSpawnLoadout(IPlayer player)
    {
        var cfg = _loadoutCFG.CurrentValue;
        if (!cfg.AutoOpenOnSpawnBeforeRoundStart || _globals.GameStart)
        {
            return false;
        }

        if (!CanUseLoadout(player, out _))
        {
            return false;
        }

        if (TryApplyRememberedLoadout(player))
        {
            return true;
        }

        return OpenStageMenu(player, LoadoutStage.Primary) != null;
    }

    private bool TryApplyRememberedLoadout(IPlayer player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return false;
        }

        var saved = _state.GetSavedPreference(player.SteamID);
        if (!saved.RememberLoadout)
        {
            return false;
        }

        bool primaryGranted = false;
        bool secondaryGranted = false;

        var primaryEntry = FindEntryById(_loadoutCFG.CurrentValue.PrimaryWeapons, saved.PrimaryLoadoutId);
        if (primaryEntry != null && TryGrantWeapon(player, primaryEntry))
        {
            var lifeState = _state.GetLifeState(player.PlayerID);
            lifeState.PrimarySelected = true;
            lifeState.PrimaryLoadoutId = ResolveEntryId(primaryEntry);
            primaryGranted = true;
        }

        var secondaryEntry = FindEntryById(_loadoutCFG.CurrentValue.SecondaryWeapons, saved.SecondaryLoadoutId);
        if (primaryGranted && secondaryEntry != null && TryGrantWeapon(player, secondaryEntry))
        {
            var lifeState = _state.GetLifeState(player.PlayerID);
            lifeState.SecondarySelected = true;
            lifeState.SecondaryLoadoutId = ResolveEntryId(secondaryEntry);
            secondaryGranted = true;
        }

        if (primaryGranted && secondaryGranted)
        {
            _helpers.SendChatT(player, "LoadoutRememberApplied");
            return true;
        }

        if (primaryGranted)
        {
            _core.Scheduler.NextTick(() =>
            {
                if (player != null && player.IsValid)
                {
                    OpenStageMenu(player, LoadoutStage.Secondary);
                }
            });
            return true;
        }

        return false;
    }

    private IMenuAPI? OpenStageMenu(IPlayer player, LoadoutStage stage)
    {
        var entries = GetEntries(stage).ToList();
        if (entries.Count == 0)
        {
            _helpers.SendChatT(player, "LoadoutMenuEmpty");
            return null;
        }

        var titleKey = stage == LoadoutStage.Primary ? "LoadoutMenuPrimaryTitle" : "LoadoutMenuSecondaryTitle";
        var promptKey = stage == LoadoutStage.Primary ? "LoadoutMenuPrimarySelect" : "LoadoutMenuSecondarySelect";
        IMenuAPI menu = _menuHelper.CreateMenu(_helpers.T(player, titleKey));

        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            _helpers.T(player, promptKey),
            Color.Red, Color.LightBlue, Color.Red),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        menu.AddOption(CreateRememberToggleButton(player, stage));

        foreach (var entry in entries)
        {
            string entryId = ResolveEntryId(entry);
            string label = BuildEntryLabel(player, stage, entry, entryId);
            var button = new ButtonMenuOption(label)
            {
                TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
                CloseAfterClick = true,
                Tag = "extend"
            };

            button.Click += async (_, args) =>
            {
                var clicker = args.Player;
                _core.Scheduler.NextTick(() => HandleSelection(clicker, stage, entry));
            };

            menu.AddOption(button);
        }

        _core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }

    private ButtonMenuOption CreateRememberToggleButton(IPlayer player, LoadoutStage stage)
    {
        var saved = _state.GetSavedPreference(player.SteamID);
        string stateText = saved.RememberLoadout
            ? _helpers.T(player, "LoadoutRememberOn")
            : _helpers.T(player, "LoadoutRememberOff");

        string fullText = $"{_helpers.T(player, "LoadoutRememberToggle")} {stateText}";
        string coloredText = saved.RememberLoadout
            ? $"<font color='#55ff55'>{fullText}</font>"
            : $"<font color='#ff5555'>{fullText}</font>";

        var button = new ButtonMenuOption(coloredText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true,
            Tag = "extend"
        };

        button.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() => ToggleRememberLoadout(clicker, stage));
        };

        return button;
    }

    private string BuildEntryLabel(IPlayer player, LoadoutStage stage, HZPLoadoutEntry entry, string entryId)
    {
        var saved = _state.GetSavedPreference(player.SteamID);
        var life = _state.GetLifeState(player.PlayerID);

        bool isSelected = stage == LoadoutStage.Primary
            ? life.PrimarySelected && life.PrimaryLoadoutId == entryId
            : life.SecondarySelected && life.SecondaryLoadoutId == entryId;

        bool isRemembered = stage == LoadoutStage.Primary
            ? saved.PrimaryLoadoutId == entryId
            : saved.SecondaryLoadoutId == entryId;

        if (isSelected)
        {
            return $"{entry.DisplayName} {_helpers.T(player, "LoadoutSelectedMarker")}";
        }

        if (saved.RememberLoadout && isRemembered)
        {
            return $"{entry.DisplayName} {_helpers.T(player, "LoadoutRememberMarker")}";
        }

        return entry.DisplayName;
    }

    private IEnumerable<HZPLoadoutEntry> GetEntries(LoadoutStage stage)
    {
        var source = stage == LoadoutStage.Primary
            ? _loadoutCFG.CurrentValue.PrimaryWeapons
            : _loadoutCFG.CurrentValue.SecondaryWeapons;

        return source
            .Where(entry => entry.Enable)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.DisplayName))
            .Where(entry => HasGrantSource(entry))
            .Where(entry => IsModeAllowed(entry.AllowedModes))
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasGrantSource(HZPLoadoutEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.WeaponCommand)
            || !string.IsNullOrWhiteSpace(entry.NativeWeaponClassName);
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

            if (Enum.TryParse<GameModeType>(modeName, true, out var mode) && mode == _gameMode.CurrentMode)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleSelection(IPlayer player, LoadoutStage stage, HZPLoadoutEntry entry)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        if (!CanUseLoadout(player, out var denyKey))
        {
            if (!string.IsNullOrWhiteSpace(denyKey))
            {
                _helpers.SendChatT(player, denyKey);
            }

            return;
        }

        if (!IsModeAllowed(entry.AllowedModes))
        {
            _helpers.SendChatT(player, "LoadoutMenuModeLocked");
            return;
        }

        if (!TryGrantWeapon(player, entry))
        {
            _helpers.SendChatT(player, "LoadoutMenuUnavailable");
            return;
        }

        string entryId = ResolveEntryId(entry);
        var lifeState = _state.GetLifeState(player.PlayerID);
        var saved = _state.GetSavedPreference(player.SteamID);

        if (stage == LoadoutStage.Primary)
        {
            lifeState.PrimarySelected = true;
            lifeState.PrimaryLoadoutId = entryId;
            lifeState.SecondarySelected = false;
            lifeState.SecondaryLoadoutId = string.Empty;

            _state.SetSavedPreference(player.SteamID, saved.RememberLoadout, entryId, saved.SecondaryLoadoutId);
            _playerDataService.SaveLoadoutPreference(player.SteamID, saved.RememberLoadout, entryId, saved.SecondaryLoadoutId);

            _helpers.SendChatT(player, "LoadoutPrimarySelected", entry.DisplayName);
            _core.Scheduler.NextTick(() =>
            {
                if (player != null && player.IsValid)
                {
                    OpenStageMenu(player, LoadoutStage.Secondary);
                }
            });
            return;
        }

        lifeState.SecondarySelected = true;
        lifeState.SecondaryLoadoutId = entryId;

        _state.SetSavedPreference(player.SteamID, saved.RememberLoadout, saved.PrimaryLoadoutId, entryId);
        _playerDataService.SaveLoadoutPreference(player.SteamID, saved.RememberLoadout, saved.PrimaryLoadoutId, entryId);
        _helpers.SendChatT(player, "LoadoutSecondarySelected", entry.DisplayName);
        _helpers.SendChatT(player, "LoadoutMenuReady");
    }

    private void ToggleRememberLoadout(IPlayer player, LoadoutStage stage)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return;
        }

        if (!CanUseLoadout(player, out var denyKey))
        {
            if (!string.IsNullOrWhiteSpace(denyKey))
            {
                _helpers.SendChatT(player, denyKey);
            }

            return;
        }

        var saved = _state.GetSavedPreference(player.SteamID);
        bool remember = !saved.RememberLoadout;
        _state.SetSavedPreference(player.SteamID, remember, saved.PrimaryLoadoutId, saved.SecondaryLoadoutId);
        _playerDataService.SaveLoadoutPreference(player.SteamID, remember, saved.PrimaryLoadoutId, saved.SecondaryLoadoutId);

        string messageKey = remember ? "LoadoutRememberEnabled" : "LoadoutRememberDisabled";
        _helpers.SendChatT(player, messageKey);
        OpenStageMenu(player, stage);
    }

    private bool TryGrantWeapon(IPlayer player, HZPLoadoutEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.WeaponCommand))
        {
            player.ExecuteCommand(entry.WeaponCommand);
            return true;
        }

        if (string.IsNullOrWhiteSpace(entry.NativeWeaponClassName))
        {
            return false;
        }

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid)
        {
            return false;
        }

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null || !weaponServices.IsValid)
        {
            return false;
        }

        var itemServices = pawn.ItemServices;
        if (itemServices == null || !itemServices.IsValid)
        {
            return false;
        }

        if (!TryGetGearSlot(entry.NativeWeaponSlot, out var gearSlot))
        {
            _logger.LogWarning("Invalid native weapon slot {Slot} for {DisplayName}", entry.NativeWeaponSlot, entry.DisplayName);
            return false;
        }

        CleanupDroppedWeaponInSlot(weaponServices, gearSlot);
        var weapon = itemServices.GiveItem<CCSWeaponBase>(entry.NativeWeaponClassName);
        return weapon != null && weapon.IsValid;
    }

    private void CleanupDroppedWeaponInSlot(CCSPlayer_WeaponServices weaponServices, gear_slot_t gearSlot)
    {
        var weaponsToCleanup = new List<CBasePlayerWeapon>();
        var myWeapons = weaponServices.MyWeapons;
        foreach (var weaponHandle in myWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon == null || !weapon.IsValid)
            {
                continue;
            }

            if (!IsWeaponInGearSlot(weapon.DesignerName, gearSlot))
            {
                continue;
            }

            weaponsToCleanup.Add(weapon);
        }

        if (weaponsToCleanup.Count == 0)
        {
            return;
        }

        weaponServices.DropWeaponBySlot(gearSlot);
        _core.Scheduler.NextTick(() =>
        {
            foreach (var weapon in weaponsToCleanup)
            {
                if (weapon == null || !weapon.IsValid)
                {
                    continue;
                }

                weapon.AcceptInput("Kill", string.Empty);
            }
        });
    }

    private static bool IsWeaponInGearSlot(string? weaponName, gear_slot_t gearSlot)
    {
        if (string.IsNullOrWhiteSpace(weaponName))
        {
            return false;
        }

        return gearSlot switch
        {
            gear_slot_t.GEAR_SLOT_RIFLE => PrimaryWeaponNames.Contains(weaponName),
            gear_slot_t.GEAR_SLOT_PISTOL => SecondaryWeaponNames.Contains(weaponName),
            _ => false
        };
    }

    private static bool TryGetGearSlot(int slot, out gear_slot_t gearSlot)
    {
        gearSlot = slot switch
        {
            0 => gear_slot_t.GEAR_SLOT_RIFLE,
            1 => gear_slot_t.GEAR_SLOT_PISTOL,
            2 => gear_slot_t.GEAR_SLOT_KNIFE,
            3 => gear_slot_t.GEAR_SLOT_GRENADES,
            _ => gear_slot_t.GEAR_SLOT_RIFLE
        };

        return slot is >= 0 and <= 3;
    }

    private static string ResolveEntryId(HZPLoadoutEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Id))
        {
            return entry.Id.Trim();
        }

        if (!string.IsNullOrWhiteSpace(entry.NativeWeaponClassName))
        {
            return entry.NativeWeaponClassName.Trim();
        }

        return entry.WeaponCommand.Trim();
    }

    private HZPLoadoutEntry? FindEntryById(IEnumerable<HZPLoadoutEntry> entries, string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return null;
        }

        return entries
            .Where(entry => entry.Enable)
            .Where(entry => HasGrantSource(entry))
            .Where(entry => IsModeAllowed(entry.AllowedModes))
            .FirstOrDefault(entry => string.Equals(ResolveEntryId(entry), entryId, StringComparison.OrdinalIgnoreCase));
    }

    private bool CanUseLoadout(IPlayer player, out string denyKey)
    {
        denyKey = string.Empty;
        var cfg = _loadoutCFG.CurrentValue;

        if (!cfg.Enable)
        {
            denyKey = "LoadoutMenuDisabled";
            return false;
        }

        if (player == null || !player.IsValid)
        {
            return false;
        }

        var controller = player.Controller;
        if (controller == null || !controller.IsValid)
        {
            return false;
        }

        if (cfg.AllowOnlyAliveHuman && !controller.PawnIsAlive)
        {
            denyKey = "LoadoutMenuAliveOnly";
            return false;
        }

        _globals.IsZombie.TryGetValue(player.PlayerID, out bool isZombie);
        if (isZombie)
        {
            denyKey = "LoadoutMenuHumanOnly";
            return false;
        }

        if (!_globals.GameStart && !cfg.AllowDuringPrep)
        {
            denyKey = "LoadoutMenuPrepLocked";
            return false;
        }

        if (_globals.GameStart && !cfg.AllowAfterGameStart)
        {
            denyKey = "LoadoutMenuRoundLocked";
            return false;
        }

        if (cfg.DenySpecialHumans)
        {
            _globals.IsSurvivor.TryGetValue(player.PlayerID, out bool isSurvivor);
            _globals.IsSniper.TryGetValue(player.PlayerID, out bool isSniper);
            _globals.IsHero.TryGetValue(player.PlayerID, out bool isHero);
            if (isSurvivor || isSniper || isHero)
            {
                denyKey = "LoadoutMenuSpecialHumanLocked";
                return false;
            }
        }

        return true;
    }
}
