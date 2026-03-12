using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HanZombiePlayerData.Contracts;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Services;

namespace HanZombiePlayerData.Provider;

[PluginMetadata(
    Id = "HanZombiePlayerData",
    Version = "1.0",
    Name = "Han Zombie Player Data",
    Author = "GitHub Copilot",
    Description = "Shared player data provider for zombie plugins.")]
public sealed class HanZombiePlayerDataPlugin(ISwiftlyCore core) : BasePlugin(core)
{
    private static readonly ZombiePlayerDataApi _apiInstance = new();

    private ServiceProvider? _serviceProvider;

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IZombiePlayerDataService, ZombiePlayerDataApi>(
            ZombiePlayerDataApi.SharedInterfaceKey,
            _apiInstance);
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration.InitializeJsonWithModel<HanZombiePlayerDataConfig>("HanZombiePlayerDataCFG.jsonc", "HanZombiePlayerDataCFG")
            .Configure(builder => builder.AddJsonFile("HanZombiePlayerDataCFG.jsonc", false, true));

        var collection = new ServiceCollection();
        collection.AddSwiftly(Core);
        collection
            .AddOptionsWithValidateOnStart<HanZombiePlayerDataConfig>()
            .BindConfiguration("HanZombiePlayerDataCFG");
        collection.AddSingleton<ZombiePlayerDataRepository>();

        _serviceProvider = collection.BuildServiceProvider();

        var repository = _serviceProvider.GetRequiredService<ZombiePlayerDataRepository>();
        _apiInstance.Initialize(
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ZombiePlayerDataApi>>(),
            repository);

        var config = _serviceProvider.GetRequiredService<IOptionsMonitor<HanZombiePlayerDataConfig>>().CurrentValue;
        if (config.BootstrapSchema)
        {
            repository.EnsureSchemaAsync().GetAwaiter().GetResult();
        }
    }

    public override void Unload()
    {
        _apiInstance.Dispose();
        _serviceProvider?.Dispose();
    }
}