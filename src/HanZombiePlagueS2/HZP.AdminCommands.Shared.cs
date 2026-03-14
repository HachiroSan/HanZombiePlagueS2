using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private bool HasAdminAccess(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
            return true;

        string permissions = mainCFG.CurrentValue.AdminMenuPermission;
        if (string.IsNullOrWhiteSpace(permissions) || permissionService.HasAnyPermission(context.Sender!, permissions))
            return true;

        helpers.SendChatT(context.Sender, "NoPermission");
        return false;
    }

    private bool HasRestrictedAdminAccess(ICommandContext context, params string[] permissions)
    {
        if (!context.IsSentByPlayer)
            return true;

        if (permissions.Any(permission => permissionService.HasPermission(context.Sender!, permission)))
            return true;

        string adminMenuPermissions = mainCFG.CurrentValue.AdminMenuPermission;
        if (!string.IsNullOrWhiteSpace(adminMenuPermissions) && permissionService.HasAnyPermission(context.Sender!, adminMenuPermissions))
            return true;

        helpers.SendChatT(context.Sender, "NoPermission");
        return false;
    }

    private bool RequirePlayerSender(ICommandContext context)
    {
        if (context.IsSentByPlayer)
            return true;

        Reply(context, "AdminCommandPlayerOnly");
        return false;
    }

    private bool RequireArgs(ICommandContext context, string commandName, string syntax, int requiredArgs)
    {
        if (context.Args.Length >= requiredArgs)
            return true;

        ReplySyntax(context, commandName, syntax);
        return false;
    }

    private bool TryParseInt(ICommandContext context, string value, string commandName, string syntax, int min, int max, out int result)
    {
        if (int.TryParse(value, out result) && result >= min && result <= max)
            return true;

        ReplySyntax(context, commandName, syntax);
        return false;
    }

    private bool TryParseFloat(ICommandContext context, string value, string commandName, string syntax, float min, float max, out float result)
    {
        if (float.TryParse(value, out result) && result >= min && result <= max)
            return true;

        ReplySyntax(context, commandName, syntax);
        return false;
    }

    private bool TryParseSteamId(ICommandContext context, string value, string commandName, string syntax, out ulong steamId)
    {
        if (ulong.TryParse(value, out steamId) && steamId != 0)
            return true;

        ReplySyntax(context, commandName, syntax);
        return false;
    }

    private bool TryParseDuration(ICommandContext context, string value, string commandName, string syntax, out TimeSpan duration)
    {
        if (HZPBanDurationParser.TryParse(value, out duration))
            return true;

        Reply(context, "AdminBanInvalidDuration", value, commandName, syntax);
        return false;
    }

    private bool TryParseIpAddress(ICommandContext context, string value, string commandName, string syntax, out string ipAddress)
    {
        if (System.Net.IPAddress.TryParse(value, out var address))
        {
            ipAddress = address.ToString();
            return true;
        }

        ipAddress = string.Empty;
        Reply(context, "AdminBanInvalidIp", value, commandName, syntax);
        return false;
    }

    private void ReplySyntax(ICommandContext context, string commandName, string syntax)
    {
        Reply(context, "AdminCommandSyntax", commandName, syntax);
    }

    private List<IPlayer>? FindTargetPlayers(ICommandContext context, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            Reply(context, "AdminCommandTargetNotFound", target);
            return null;
        }

        var players = core.PlayerManager.FindTargettedPlayers(context.Sender!, target, TargetSearchMode.IncludeSelf)
            ?.Where(player => player != null && player.IsValid)
            .ToList();

        if (players == null || players.Count == 0)
        {
            Reply(context, "AdminCommandTargetNotFound", target);
            return null;
        }

        return players;
    }

    private List<IPlayer>? FindLiveTargetPlayers(
        ICommandContext context,
        string target,
        Func<IPlayer, bool>? predicate,
        string invalidStateKey)
    {
        var players = FindTargetPlayers(context, target)
            ?.Where(player => IsAlivePawn(player.PlayerPawn));

        if (predicate != null)
        {
            players = players?.Where(predicate);
        }

        var targets = players?.ToList();
        if (targets == null || targets.Count == 0)
        {
            Reply(context, invalidStateKey);
            return null;
        }

        return targets;
    }

    private List<IPlayer>? FindEconomyTargetPlayers(ICommandContext context, string target)
    {
        var targets = FindTargetPlayers(context, target)
            ?.Where(player => player.SteamID != 0)
            .ToList();

        if (targets == null || targets.Count == 0)
        {
            Reply(context, "AdminCommandTargetNotFound", target);
            return null;
        }

        return targets;
    }

    private bool TryGetAliveSender(ICommandContext context, string errorKey, out IPlayer sender, out CCSPlayerPawn pawn)
    {
        sender = context.Sender!;
        pawn = sender.PlayerPawn!;

        if (!IsAlivePawn(sender.PlayerPawn))
        {
            Reply(context, errorKey);
            return false;
        }

        return true;
    }

    private bool TryGetTargetPosition(
        ICommandContext context,
        IPlayer target,
        string deadErrorKey,
        string invalidPositionErrorKey,
        out Vector origin,
        out QAngle rotation)
    {
        origin = default;
        rotation = default;

        var targetPawn = target.PlayerPawn;
        if (!IsAlivePawn(targetPawn))
        {
            Reply(context, deadErrorKey);
            return false;
        }

        var absOrigin = targetPawn!.AbsOrigin;
        var absRotation = targetPawn.AbsRotation;
        if (absOrigin == null || absRotation == null)
        {
            Reply(context, invalidPositionErrorKey);
            return false;
        }

        origin = absOrigin.Value;
        rotation = absRotation.Value;
        return true;
    }

    private void Reply(ICommandContext context, string key, params object[] args)
    {
        if (context.IsSentByPlayer)
        {
            helpers.SendChatT(context.Sender, key, args);
            return;
        }

        context.Reply(core.Localizer[key, args]);
    }

    private void NotifyTarget(ICommandContext context, IPlayer target, string key, params object[] args)
    {
        if (!context.IsSentByPlayer && target.IsFakeClient)
            return;

        helpers.SendChatT(target, key, args);
    }

    private static bool IsAlivePawn(CCSPlayerPawn? pawn)
    {
        return pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    private string GetActorName(ICommandContext context)
    {
        if (!context.IsSentByPlayer || context.Sender?.Controller == null || !context.Sender.Controller.IsValid)
            return "Console";

        return context.Sender.Controller.PlayerName;
    }

    private string LocalizeRole(ICommandContext context, string key)
    {
        if (context.IsSentByPlayer)
            return helpers.T(context.Sender, key);

        return core.Localizer[key];
    }

    private float GetDefaultDuration(StoreGrantType grantType, float fallback)
    {
        var item = storeCFG.CurrentValue.ItemList.FirstOrDefault(entry => entry.Enable && entry.GrantType == grantType && entry.FloatValue > 0);
        return item?.FloatValue > 0 ? item.FloatValue : fallback;
    }

    private int GetDefaultAmount(StoreGrantType grantType, int fallback)
    {
        var item = storeCFG.CurrentValue.ItemList.FirstOrDefault(entry => entry.Enable && entry.GrantType == grantType && entry.IntValue > 0);
        return item?.IntValue > 0 ? item.IntValue : fallback;
    }

    private static string FormatSeconds(float seconds)
    {
        return seconds.ToString("0.#");
    }

    private static string GetPlayerName(IPlayer player)
    {
        if (player.Controller != null && player.Controller.IsValid)
            return player.Controller.PlayerName;

        return player.Name;
    }

    private static string FormatPlayerList(IEnumerable<IPlayer> players)
    {
        return string.Join(", ", players.Select(GetPlayerName));
    }
}
