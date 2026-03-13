using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

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
    private HZPPlayerDataService? _playerDataService;

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
        Core.Configuration.InitializeJsonWithModel<HZPLoadoutCFG>("HZPLoadoutCFG.jsonc", "HZPLoadoutCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPLoadoutCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPEconomyCFG>("HZPEconomyCFG.jsonc", "HZPEconomyCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPEconomyCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPStoreCFG>("HZPStoreCFG.jsonc", "HZPStoreCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPStoreCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPChatCFG>("HZPChatCFG.jsonc", "HZPChatCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPChatCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPBroadcastCFG>("HZPBroadcastCFG.jsonc", "HZPBroadcastCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPBroadcastCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPMapVoteCFG>("HZPMapVoteCFG.jsonc", "HZPMapVoteCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPMapVoteCFG.jsonc", false, true);
        });
        Core.Configuration.InitializeJsonWithModel<HZPDatabaseConfig>("HZPDatabaseCFG.jsonc", "HZPDatabaseCFG").Configure(builder =>
        {
            builder.AddJsonFile("HZPDatabaseCFG.jsonc", false, true);
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
            .AddOptionsWithValidateOnStart<HZPLoadoutCFG>()
            .BindConfiguration("HZPLoadoutCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPEconomyCFG>()
            .BindConfiguration("HZPEconomyCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPStoreCFG>()
            .BindConfiguration("HZPStoreCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPChatCFG>()
            .BindConfiguration("HZPChatCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPBroadcastCFG>()
            .BindConfiguration("HZPBroadcastCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPMapVoteCFG>()
            .BindConfiguration("HZPMapVoteCFG");

        collection
            .AddOptionsWithValidateOnStart<HZPDatabaseConfig>()
            .BindConfiguration("HZPDatabaseCFG");

        collection.AddSingleton<HZPGlobals>();
        collection.AddSingleton<HZPDatabaseRepository>();
        collection.AddSingleton<HZPDatabaseService>();
        collection.AddSingleton<HZPEconomyState>();
        collection.AddSingleton<HZPEconomyService>();
        collection.AddSingleton<HZPEvents>();
        collection.AddSingleton<HZPHelpers>();
        collection.AddSingleton<HZPServices>();
        collection.AddSingleton<HZPCommands>();
        collection.AddSingleton<PlayerZombieState>();
        collection.AddSingleton<HZPPlayerDataService>();
        collection.AddSingleton<HZPMenuHelper>();
        collection.AddSingleton<HZPZombieClassMenu>();
        collection.AddSingleton<HZPAdminItemMenu>();
        collection.AddSingleton<HZPLoadoutState>();
        collection.AddSingleton<HZPLoadoutMenu>();
        collection.AddSingleton<HZPStoreState>();
        collection.AddSingleton<HZPStoreService>();
        collection.AddSingleton<HZPStoreMenu>();
        collection.AddSingleton<HZPBroadcastGeoIpService>();
        collection.AddSingleton<HZPBroadcastCountryService>();
        collection.AddSingleton<HZPBroadcastState>();
        collection.AddSingleton<HZPBroadcastService>();
        collection.AddSingleton<HZPMapVoteState>();
        collection.AddSingleton<HZPMapVoteService>();
        collection.AddSingleton<HZPMapVoteMenu>();
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
        ServiceProvider.GetRequiredService<HZPMapVoteService>().SetMenu(ServiceProvider.GetRequiredService<HZPMapVoteMenu>());
        ServiceProvider.GetRequiredService<HZPBroadcastService>().Start();
        _playerDataService = ServiceProvider.GetRequiredService<HZPPlayerDataService>();
        var databaseConfig = ServiceProvider.GetRequiredService<IOptionsMonitor<HZPDatabaseConfig>>().CurrentValue;
        if (databaseConfig.BootstrapSchema)
        {
            ServiceProvider.GetRequiredService<HZPDatabaseService>().EnsureSchemaAsync().GetAwaiter().GetResult();
        }

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
        _playerDataService.LoadOnlinePlayers();
    }


    public override void Unload()
    {
        _apiInstance.HZP_OnPreferenceChanged -= HandlePreferenceChanged;
        _apiInstance.HZP_OnGameStart -= HandleGameStartChanged;
        _apiInstance.HZP_OnPlayerInfect -= HandlePlayerInfect;
        _apiInstance.HZP_OnHumanWin -= HandleRoundOutcome;
        _apiInstance!.Dispose();
        ServiceProvider!.Dispose();
    }

    private void HandlePreferenceChanged(ulong steamId, string? className)
    {
        _playerDataService?.SavePreference(steamId, className);
    }

    private void HandleGameStartChanged(bool gameStart)
    {
        _playerDataService?.HandleGameStartChanged(gameStart);
    }

    private void HandlePlayerInfect(IPlayer attacker, IPlayer victim, bool grenade, string zombieClassName)
    {
        _playerDataService?.RecordInfection(attacker, victim);
    }

    private void HandleRoundOutcome(bool humanWon)
    {
        _playerDataService?.RecordRoundOutcome(humanWon);
    }

    
}
