using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HanZombiePlayerData.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Services;

namespace HanZombiePlagueS2;

[PluginMetadata(
    Id = "HanZombiePlagueS2",
    Version = "1.0",
    Name = "CS2 僵尸瘟疫 for Sw2/CS2 ZombiePlague for Sw2",
    Author = "H-AN",
    Description = "CS2 僵尸瘟疫 SW2版本 CS2 ZombiePlague for SW2.")]

public partial class HanZombiePlagueS2(ISwiftlyCore core) : BasePlugin(core)
{

    private ServiceProvider? ServiceProvider { get; set; }
    private static readonly HanZombiePlagueAPI _apiInstance = new();
    private HZPMainCFG _HZPMainCFG = null!;
    private HZPGlobals _Globals = null!;
    private HZPEvents _Events = null!;
    private HZPCommands _Commands = null!;
    private ZombiePlayerDataBridge? _playerDataBridge;

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IHanZombiePlagueAPI, HanZombiePlagueAPI>("HanZombiePlague", _apiInstance);
    }
    public override void Load(bool hotReload)
    {
        Core.Configuration.InitializeJsonWithModel<HZPMainCFG>("HZPMainCFG.jsonc", "HZPMainCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPMainCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPVoxCFG>("HZPVoxCFG.jsonc", "HZPVoxCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPVoxCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPZombieClassCFG>("HZPZombieClassCFG.jsonc", "HZPZombieClassCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPZombieClassCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPSpecialClassCFG>("HZPSpecialClassCFG.jsonc", "HZPSpecialClassCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPSpecialClassCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPWeaponMenuCFG>("HZPWeaponMenuCFG.jsonc", "HZPWeaponMenuCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPWeaponMenuCFG.jsonc", false, true);
        });

        
        var collection = new ServiceCollection();
        collection.AddSwiftly(Core);
        collection.AddSingleton<IHanZombiePlagueAPI>(_apiInstance);
        collection.AddSingleton(_apiInstance);

        collection
            .AddOptionsWithValidateOnStart<HZPMainCFG>()
            .BindConfiguration("HZPMainCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPVoxCFG>()
            .BindConfiguration("HZPVoxCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPZombieClassCFG>()
            .BindConfiguration("HZPZombieClassCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPSpecialClassCFG>()
            .BindConfiguration("HZPSpecialClassCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPWeaponMenuCFG>()
            .BindConfiguration("HZPWeaponMenuCFG");

        collection.AddSingleton<HZPGlobals>();
        collection.AddSingleton<HZPEvents>();
        collection.AddSingleton<HZPHelpers>();
        collection.AddSingleton<HZPServices>();
        collection.AddSingleton<HZPCommands>();
        collection.AddSingleton<PlayerZombieState>();
        collection.AddSingleton<ZombiePlayerDataBridge>();
        collection.AddSingleton<HZPMenuHelper>();
        collection.AddSingleton<HZPZombieClassMenu>();
        collection.AddSingleton<HZPAdminItemMenu>();
        collection.AddSingleton<HZPWeaponMenuState>();
        collection.AddSingleton<HZPWeaponMenu>();
        collection.AddSingleton<HZPGameMode>();


        ServiceProvider = collection.BuildServiceProvider();

        _apiInstance.Initialize(
            Core,
            ServiceProvider.GetRequiredService<ILogger<HanZombiePlagueAPI>>(),
            ServiceProvider.GetRequiredService<HZPGlobals>(),
            ServiceProvider.GetRequiredService<HZPHelpers>(),
            ServiceProvider.GetRequiredService<HZPServices>(),
            ServiceProvider.GetRequiredService<IOptionsMonitor<HZPMainCFG>>(),
            ServiceProvider.GetRequiredService<PlayerZombieState>(),
            ServiceProvider.GetRequiredService<IOptionsMonitor<HZPZombieClassCFG>>(),
            ServiceProvider.GetRequiredService<IOptionsMonitor<HZPSpecialClassCFG>>(),
            ServiceProvider.GetRequiredService<HZPGameMode>()
        );

        _Globals = ServiceProvider.GetRequiredService<HZPGlobals>();
        _Events = ServiceProvider.GetRequiredService<HZPEvents>();
        _Commands = ServiceProvider.GetRequiredService<HZPCommands>();
        _playerDataBridge = ServiceProvider.GetRequiredService<ZombiePlayerDataBridge>();

        var ZriotCFGMonitor = ServiceProvider.GetRequiredService<IOptionsMonitor<HZPMainCFG>>();
        _HZPMainCFG = ZriotCFGMonitor.CurrentValue;
        ZriotCFGMonitor.OnChange(newConfig =>
        {
            _HZPMainCFG = newConfig;
            Core.Logger.LogInformation(Core.Localizer["ServerInfoHotReload"]); 
        });

        _Events.HookEvents();
        _Events.HookZombieSoundEvents();
        _Commands.Command();
        _Commands.MenuCommands();

        _apiInstance.HZP_OnPreferenceChanged += HandlePreferenceChanged;
        _apiInstance.HZP_OnGameStart += HandleGameStartChanged;
        _apiInstance.HZP_OnPlayerInfect += HandlePlayerInfect;
        _apiInstance.HZP_OnHumanWin += HandleRoundOutcome;
    }

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        BindPlayerDataService(interfaceManager);
    }

    public override void OnSharedInterfaceInjected(IInterfaceManager interfaceManager)
    {
        BindPlayerDataService(interfaceManager);
    }


    public override void Unload()
    {
        _apiInstance.HZP_OnPreferenceChanged -= HandlePreferenceChanged;
        _apiInstance.HZP_OnGameStart -= HandleGameStartChanged;
        _apiInstance.HZP_OnPlayerInfect -= HandlePlayerInfect;
        _apiInstance.HZP_OnHumanWin -= HandleRoundOutcome;
        _playerDataBridge?.SetService(null);
        _apiInstance!.Dispose();
        ServiceProvider!.Dispose();
    }

    private void BindPlayerDataService(IInterfaceManager interfaceManager)
    {
        if (_playerDataBridge == null)
        {
            return;
        }

        if (!interfaceManager.HasSharedInterface(ZombiePlayerDataBridge.SharedInterfaceKey))
        {
            _playerDataBridge.SetService(null);
            return;
        }

        var dataService = interfaceManager.GetSharedInterface<IZombiePlayerDataService>(ZombiePlayerDataBridge.SharedInterfaceKey);
        _playerDataBridge.SetService(dataService);
        _playerDataBridge.LoadOnlinePlayers();
    }

    private void HandlePreferenceChanged(ulong steamId, string? className)
    {
        _playerDataBridge?.SavePreference(steamId, className);
    }

    private void HandleGameStartChanged(bool gameStart)
    {
        _playerDataBridge?.HandleGameStartChanged(gameStart);
    }

    private void HandlePlayerInfect(IPlayer attacker, IPlayer victim, bool grenade, string zombieClassName)
    {
        _playerDataBridge?.RecordInfection(attacker, victim);
    }

    private void HandleRoundOutcome(bool humanWon)
    {
        _playerDataBridge?.RecordRoundOutcome(humanWon);
    }

    
}
