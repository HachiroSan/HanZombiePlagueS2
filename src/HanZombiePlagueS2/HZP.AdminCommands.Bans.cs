using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private const string BanPermission = "hzp.admin.ban";
    private const string GlobalBanPermission = "hzp.admin.ban.global";
    private const string UnbanPermission = "hzp.admin.unban";
    private const string GlobalUnbanPermission = "hzp.admin.unban.global";

    private void BanCommand(ICommandContext context) => ExecuteSteamBanCommand(context, BanCommandName, false);
    private void GlobalBanCommand(ICommandContext context) => ExecuteSteamBanCommand(context, GlobalBanCommandName, true);
    private void BanIpCommand(ICommandContext context) => ExecuteIpBanCommand(context, BanIpCommandName, false);
    private void GlobalBanIpCommand(ICommandContext context) => ExecuteIpBanCommand(context, GlobalBanIpCommandName, true);
    private void UnbanCommand(ICommandContext context) => ExecuteUnbanCommand(context, UnbanCommandName, false);
    private void GlobalUnbanCommand(ICommandContext context) => ExecuteUnbanCommand(context, GlobalUnbanCommandName, true);
    private void UnbanIpCommand(ICommandContext context) => ExecuteUnbanIpCommand(context, UnbanIpCommandName, false);
    private void GlobalUnbanIpCommand(ICommandContext context) => ExecuteUnbanIpCommand(context, GlobalUnbanIpCommandName, true);

    private void ExecuteSteamBanCommand(ICommandContext context, string commandName, bool global)
    {
        string syntax = "<player|steamid64> <duration> <reason>";
        if (!HasRestrictedAdminAccess(context, global ? GlobalBanPermission : BanPermission))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 3))
            return;

        if (!TryParseDuration(context, context.Args[1], commandName, syntax, out TimeSpan duration))
            return;

        string reason = string.Join(" ", context.Args.Skip(2)).Trim();
        if (reason.Length == 0)
        {
            ReplySyntax(context, commandName, syntax);
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0]);
        if (targets != null && targets.Count > 0)
        {
            _ = ApplySteamBanTargetsAsync(context, targets, duration, reason, global);
            return;
        }

        if (!TryParseSteamId(context, context.Args[0], commandName, syntax, out ulong steamId))
            return;

        _ = ApplyOfflineSteamBanAsync(context, steamId, duration, reason, global);
    }

    private void ExecuteIpBanCommand(ICommandContext context, string commandName, bool global)
    {
        string syntax = "<player|ip> <duration> <reason>";
        if (!HasRestrictedAdminAccess(context, global ? GlobalBanPermission : BanPermission))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 3))
            return;

        if (!TryParseDuration(context, context.Args[1], commandName, syntax, out TimeSpan duration))
            return;

        string reason = string.Join(" ", context.Args.Skip(2)).Trim();
        if (reason.Length == 0)
        {
            ReplySyntax(context, commandName, syntax);
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0])
            ?.Where(player => !string.IsNullOrWhiteSpace(player.IPAddress))
            .ToList();

        if (targets != null && targets.Count > 0)
        {
            _ = ApplyIpBanTargetsAsync(context, targets, duration, reason, global);
            return;
        }

        if (!TryParseIpAddress(context, context.Args[0], commandName, syntax, out string ipAddress))
            return;

        _ = ApplyOfflineIpBanAsync(context, ipAddress, duration, reason, global);
    }

    private void ExecuteUnbanCommand(ICommandContext context, string commandName, bool globalOnly)
    {
        string syntax = "<steamid64>";
        if (!HasRestrictedAdminAccess(context, globalOnly ? GlobalUnbanPermission : UnbanPermission))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 1))
            return;

        if (!TryParseSteamId(context, context.Args[0], commandName, syntax, out ulong steamId))
            return;

        _ = UnbanSteamIdAsync(context, steamId, globalOnly);
    }

    private void ExecuteUnbanIpCommand(ICommandContext context, string commandName, bool globalOnly)
    {
        string syntax = "<ip>";
        if (!HasRestrictedAdminAccess(context, globalOnly ? GlobalUnbanPermission : UnbanPermission))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 1))
            return;

        if (!TryParseIpAddress(context, context.Args[0], commandName, syntax, out string ipAddress))
            return;

        _ = UnbanIpAsync(context, ipAddress, globalOnly);
    }

    private async Task ApplySteamBanTargetsAsync(ICommandContext context, List<IPlayer> targets, TimeSpan duration, string reason, bool global)
    {
        string actorName = GetActorName(context);
        ulong actorSteamId = context.Sender?.SteamID ?? 0;
        var bannedNames = new List<string>();

        foreach (var target in targets.Where(player => player.SteamID != 0))
        {
            bool success = await banService.BanPlayerAsync(target.SteamID, GetPlayerName(target), target.IPAddress, HZPBanType.SteamId, duration, reason, actorSteamId, actorName, global);
            if (!success)
                continue;

            bannedNames.Add(GetPlayerName(target));
            await banService.EnforceBanAsync(target);
        }

        if (bannedNames.Count == 0)
        {
            Reply(context, "AdminBanFailed");
            return;
        }

        Reply(context, global ? "AdminGlobalBanApplied" : "AdminBanApplied", string.Join(", ", bannedNames), reason, FormatBanDuration(duration));
    }

    private async Task ApplyOfflineSteamBanAsync(ICommandContext context, ulong steamId, TimeSpan duration, string reason, bool global)
    {
        string actorName = GetActorName(context);
        bool success = await banService.BanPlayerAsync(steamId, "Unknown", string.Empty, HZPBanType.SteamId, duration, reason, context.Sender?.SteamID ?? 0, actorName, global);
        if (!success)
        {
            Reply(context, "AdminBanFailed");
            return;
        }

        Reply(context, global ? "AdminGlobalBanApplied" : "AdminBanApplied", steamId.ToString(), reason, FormatBanDuration(duration));
    }

    private async Task ApplyIpBanTargetsAsync(ICommandContext context, List<IPlayer> targets, TimeSpan duration, string reason, bool global)
    {
        string actorName = GetActorName(context);
        ulong actorSteamId = context.Sender?.SteamID ?? 0;
        var bannedTargets = new List<string>();

        foreach (var target in targets)
        {
            bool success = await banService.BanPlayerAsync(target.SteamID, GetPlayerName(target), target.IPAddress, HZPBanType.IP, duration, reason, actorSteamId, actorName, global);
            if (!success)
                continue;

            bannedTargets.Add($"{GetPlayerName(target)} ({target.IPAddress})");
            await banService.EnforceBanAsync(target);
        }

        if (bannedTargets.Count == 0)
        {
            Reply(context, "AdminBanFailed");
            return;
        }

        Reply(context, global ? "AdminGlobalBanApplied" : "AdminBanApplied", string.Join(", ", bannedTargets), reason, FormatBanDuration(duration));
    }

    private async Task ApplyOfflineIpBanAsync(ICommandContext context, string ipAddress, TimeSpan duration, string reason, bool global)
    {
        string actorName = GetActorName(context);
        bool success = await banService.BanPlayerAsync(0, "Unknown", ipAddress, HZPBanType.IP, duration, reason, context.Sender?.SteamID ?? 0, actorName, global);
        if (!success)
        {
            Reply(context, "AdminBanFailed");
            return;
        }

        Reply(context, global ? "AdminGlobalBanApplied" : "AdminBanApplied", ipAddress, reason, FormatBanDuration(duration));
    }

    private async Task UnbanSteamIdAsync(ICommandContext context, ulong steamId, bool globalOnly)
    {
        int removed = await banService.UnbanSteamIdAsync(steamId, globalOnly);
        if (removed <= 0)
        {
            Reply(context, "AdminUnbanNotFound", steamId.ToString());
            return;
        }

        Reply(context, globalOnly ? "AdminGlobalUnbanApplied" : "AdminUnbanApplied", steamId.ToString(), removed);
    }

    private async Task UnbanIpAsync(ICommandContext context, string ipAddress, bool globalOnly)
    {
        int removed = await banService.UnbanIpAsync(ipAddress, globalOnly);
        if (removed <= 0)
        {
            Reply(context, "AdminUnbanNotFound", ipAddress);
            return;
        }

        Reply(context, globalOnly ? "AdminGlobalUnbanApplied" : "AdminUnbanApplied", ipAddress, removed);
    }

    private static string FormatBanDuration(TimeSpan duration)
    {
        return duration == TimeSpan.Zero ? "permanent" : duration.ToString();
    }
}
