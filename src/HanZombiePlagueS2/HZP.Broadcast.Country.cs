using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPBroadcastCountryService(
    ISwiftlyCore core,
    HZPDatabaseService databaseService,
    HZPBroadcastGeoIpService geoIpService,
    IOptionsMonitor<HZPBroadcastCFG> broadcastCFG,
    ILogger<HZPBroadcastCountryService> logger)
{
    private const string CountryCodeKey = "country_announce_country";
    private const string LastFetchKey = "country_announce_lastfetch";
    private readonly HashSet<ulong> _processingPlayers = [];
    private bool _initialized;

    private static readonly Dictionary<string, string> CountryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "United States",
        ["CA"] = "Canada",
        ["GB"] = "United Kingdom",
        ["DE"] = "Germany",
        ["FR"] = "France",
        ["NL"] = "Netherlands",
        ["SE"] = "Sweden",
        ["NO"] = "Norway",
        ["FI"] = "Finland",
        ["RU"] = "Russia",
        ["UA"] = "Ukraine",
        ["PL"] = "Poland",
        ["TR"] = "Turkey",
        ["CN"] = "China",
        ["JP"] = "Japan",
        ["KR"] = "South Korea",
        ["VN"] = "Vietnam",
        ["TH"] = "Thailand",
        ["ID"] = "Indonesia",
        ["IN"] = "India",
        ["AU"] = "Australia",
        ["BR"] = "Brazil",
        ["AR"] = "Argentina",
        ["MX"] = "Mexico",
        ["CL"] = "Chile",
        ["SA"] = "Saudi Arabia",
        ["AE"] = "United Arab Emirates",
        ["EG"] = "Egypt",
        ["PH"] = "Philippines",
        ["MY"] = "Malaysia",
        ["SG"] = "Singapore"
    };

    public async Task<string> GetCountryDisplayNameAsync(IPlayer player, bool allowUnknown)
    {
        var cfg = broadcastCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableWelcome || !cfg.EnableCountryAnnounce || player == null || !player.IsValid || player.IsFakeClient || player.SteamID == 0)
        {
            return string.Empty;
        }

        Initialize();
        try
        {
            string? countryCode = await GetCountryCodeAsync(player);
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                if (!allowUnknown)
                {
                    return string.Empty;
                }

                return cfg.UnknownCountryLabel;
            }

            return GetCountryDisplayName(countryCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CountryAnnounce] Failed to resolve country for {SteamId}", player.SteamID);
            return string.Empty;
        }
    }

    private async Task<string?> GetCountryCodeAsync(IPlayer player)
    {
        var countryPref = await databaseService.GetPlayerPreferenceAsync(player.SteamID, CountryCodeKey);
        var lastFetchPref = await databaseService.GetPlayerPreferenceAsync(player.SteamID, LastFetchKey);

        string cachedCode = countryPref?.PreferenceValue?.Trim().ToUpperInvariant() ?? string.Empty;
        DateTime lastFetch = ParseDate(lastFetchPref?.PreferenceValue);
        bool shouldFetch = string.IsNullOrWhiteSpace(cachedCode)
            || DateTime.UtcNow - lastFetch > TimeSpan.FromHours(broadcastCFG.CurrentValue.CacheExpiryHours);

        if (!shouldFetch)
        {
            return cachedCode;
        }

        if (!_processingPlayers.Add(player.SteamID))
        {
            return cachedCode;
        }

        try
        {
            string? resolvedCode = ResolveCountryCode(player);
            if (string.IsNullOrWhiteSpace(resolvedCode))
            {
                return cachedCode;
            }

            resolvedCode = resolvedCode.ToUpperInvariant();
            await databaseService.SavePlayerPreferenceAsync(player.SteamID, CountryCodeKey, resolvedCode);
            await databaseService.SavePlayerPreferenceAsync(player.SteamID, LastFetchKey, DateTime.UtcNow.ToString("O"));
            return resolvedCode;
        }
        finally
        {
            _processingPlayers.Remove(player.SteamID);
        }
    }

    private string? ResolveCountryCode(IPlayer player)
    {
        if (!string.IsNullOrWhiteSpace(broadcastCFG.CurrentValue.DebugCountryCodeOverride))
        {
            return broadcastCFG.CurrentValue.DebugCountryCodeOverride.Trim();
        }

        return geoIpService.GetCountryCode(player.IPAddress);
    }

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        string dbPath = Path.Combine(core.PluginDataDirectory, "GeoLite2-Country.mmdb");
        geoIpService.Initialize(dbPath);
    }

    private string GetCountryDisplayName(string countryCode)
    {
        return CountryNames.TryGetValue(countryCode, out var displayName) ? displayName : countryCode;
    }

    private static DateTime ParseDate(string? raw)
    {
        return DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTime.MinValue;
    }
}
