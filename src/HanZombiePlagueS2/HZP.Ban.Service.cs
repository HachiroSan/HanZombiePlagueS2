using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace HanZombiePlagueS2;

public sealed class HZPBanService(
    HZPDatabaseService databaseService,
    IOptionsMonitor<HZPBanCFG> banCFG,
    ILogger<HZPBanService> logger,
    ISwiftlyCore core)
{
    public bool IsEnabled => banCFG.CurrentValue.Enable;

    public string ServerScope => banCFG.CurrentValue.ServerScope;

    public float ConnectCheckDelaySeconds => banCFG.CurrentValue.ConnectCheckDelaySeconds;

    public async Task<bool> BanPlayerAsync(
        ulong targetSteamId,
        string? targetName,
        string? targetIp,
        HZPBanType banType,
        TimeSpan duration,
        string reason,
        ulong adminSteamId,
        string adminName,
        bool global,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (banType == HZPBanType.SteamId && targetSteamId == 0)
        {
            return false;
        }

        if (banType == HZPBanType.IP && string.IsNullOrWhiteSpace(targetIp))
        {
            return false;
        }

        try
        {
            await databaseService.AddBanAsync(new HZPBanCreateRequest
            {
                SteamId64 = (long)targetSteamId,
                PlayerName = targetName ?? string.Empty,
                PlayerIp = targetIp ?? string.Empty,
                BanType = banType,
                ExpiresAt = CalculateExpiresAt(duration),
                Length = (long)duration.TotalMilliseconds,
                Reason = reason,
                AdminSteamId64 = (long)adminSteamId,
                AdminName = adminName,
                ScopeKey = ServerScope,
                GlobalBan = global
            }, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add ban for SteamID {SteamId}.", targetSteamId);
            return false;
        }
    }

    public async Task<int> UnbanSteamIdAsync(ulong steamId, bool globalOnly, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || steamId == 0)
        {
            return 0;
        }

        try
        {
            return await databaseService.ExpireBansBySteamIdAsync(steamId, ServerScope, globalOnly, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to expire bans for SteamID {SteamId}.", steamId);
            return 0;
        }
    }

    public async Task<int> UnbanIpAsync(string playerIp, bool globalOnly, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(playerIp))
        {
            return 0;
        }

        try
        {
            return await databaseService.ExpireBansByIpAsync(playerIp, ServerScope, globalOnly, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to expire bans for IP {PlayerIp}.", playerIp);
            return 0;
        }
    }

    public async Task<HZPBanRecord?> FindActiveBanAsync(ulong steamId, string? playerIp, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return null;
        }

        try
        {
            return await databaseService.FindActiveBanAsync(steamId, playerIp, ServerScope, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query active ban for SteamID {SteamId}.", steamId);
            return null;
        }
    }

    public async Task<bool> EnforceBanAsync(IPlayer player, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || player == null || !player.IsValid || player.IsFakeClient)
        {
            return false;
        }

        var ban = await FindActiveBanAsync(player.SteamID, player.IPAddress, cancellationToken);
        if (ban == null)
        {
            return false;
        }

        string kickMessage = BuildKickMessage(player, ban);
        core.Scheduler.NextTick(() =>
        {
            if (player == null || !player.IsValid)
            {
                return;
            }

            player.SendMessage(MessageType.Console, kickMessage);
            player.KickAsync(kickMessage, ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED);
        });

        return true;
    }

    public static long CalculateExpiresAt(TimeSpan duration)
    {
        return duration == TimeSpan.Zero
            ? 0
            : DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds();
    }

    public string FormatExpiry(long expiresAt)
    {
        return expiresAt == 0
            ? "Never"
            : DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    private string BuildKickMessage(IPlayer player, HZPBanRecord ban)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);
        return localizer[
            "AdminBanKickMessage",
            ban.Reason,
            FormatExpiry(ban.ExpiresAt),
            ban.AdminName,
            ban.AdminSteamId64 == 0 ? "0" : ban.AdminSteamId64.ToString()];
    }
}
