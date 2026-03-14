using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void TVaccineCommand(ICommandContext context) =>
        ApplySimpleItemCommand(context, TVaccineCommandName, target => api.HZP_IsZombie(target.PlayerID) && !api.HZP_IsNemesis(target.PlayerID) && !api.HZP_IsAssassin(target.PlayerID), "AdminCommandItemNeedsZombie", "AdminCommandTVaccineSender", "AdminCommandTVaccineTarget", target => api.HZP_SetTargetTVaccine(target));

    private void ScbaCommand(ICommandContext context) =>
        ApplySimpleItemCommand(context, ScbaCommandName, target => !api.HZP_IsZombie(target.PlayerID) && !api.HZP_PlayerHaveScbaSuit(target.PlayerID), "AdminCommandItemNeedsHuman", "AdminCommandScbaSender", "AdminCommandScbaTarget", target => api.HZP_GiveScbaSuit(target));

    private void TVirusGrenadeCommand(ICommandContext context) =>
        ApplySimpleItemCommand(context, TVirusGrenadeCommandName, target => api.HZP_IsZombie(target.PlayerID), "AdminCommandItemNeedsZombie", "AdminCommandTVirusGrenadeSender", "AdminCommandTVirusGrenadeTarget", target => api.HZP_GiveTVirusGrenade(target));

    private void GodCommand(ICommandContext context)
    {
        float duration = GetDefaultDuration(StoreGrantType.GodMode, 20f);
        if (context.Args.Length >= 2 && !TryParseFloat(context, context.Args[1], GodCommandName, "<player> [seconds]", 0.1f, 3600f, out duration))
            return;

        ApplyTimedItemCommand(
            context,
            GodCommandName,
            "<player> [seconds]",
            target => !api.HZP_PlayerHaveGodState(target.PlayerID),
            "AdminCommandItemNeedsLiving",
            "AdminCommandGodSender",
            "AdminCommandGodTarget",
            duration,
            target => api.HZP_GiveGodState(target, duration));
    }

    private void InfiniteAmmoCommand(ICommandContext context)
    {
        float duration = GetDefaultDuration(StoreGrantType.InfiniteAmmo, 20f);
        if (context.Args.Length >= 2 && !TryParseFloat(context, context.Args[1], InfiniteAmmoCommandName, "<player> [seconds]", 0.1f, 3600f, out duration))
            return;

        ApplyTimedItemCommand(
            context,
            InfiniteAmmoCommandName,
            "<player> [seconds]",
            target => !api.HZP_IsZombie(target.PlayerID) && !api.HZP_PlayerHaveInfiniteAmmoState(target.PlayerID),
            "AdminCommandItemNeedsHuman",
            "AdminCommandInfiniteAmmoSender",
            "AdminCommandInfiniteAmmoTarget",
            duration,
            target => api.HZP_GiveInfiniteAmmo(target, duration));
    }

    private void AddHealthCommand(ICommandContext context)
    {
        int amount = GetDefaultAmount(StoreGrantType.AddHealth, 200);
        if (context.Args.Length >= 2 && !TryParseInt(context, context.Args[1], AddHealthCommandName, "<player> [amount]", 1, int.MaxValue, out amount))
            return;

        ApplyValuedItemCommand(
            context,
            AddHealthCommandName,
            "<player> [amount]",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandItemNeedsHuman",
            "AdminCommandAddHealthSender",
            "AdminCommandAddHealthTarget",
            $"{amount:N0} HP",
            target => api.HZP_HumanAddHealth(target, amount));
    }

    private void ApplySimpleItemCommand(ICommandContext context, string commandName, Func<IPlayer, bool> canApply, string invalidStateKey, string senderKey, string targetKey, Action<IPlayer> apply)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, "<player>", 1))
            return;

        var targets = FindLiveTargetPlayers(context, context.Args[0], canApply, invalidStateKey);
        if (targets == null)
            return;

        string actorName = GetActorName(context);
        foreach (var target in targets)
        {
            apply(target);
            NotifyTarget(context, target, targetKey, actorName);
        }

        Reply(context, senderKey, FormatPlayerList(targets));
    }

    private void ApplyTimedItemCommand(ICommandContext context, string commandName, string syntax, Func<IPlayer, bool> canApply, string invalidStateKey, string senderKey, string targetKey, float duration, Action<IPlayer> apply)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 1))
            return;

        var targets = FindLiveTargetPlayers(context, context.Args[0], canApply, invalidStateKey);
        if (targets == null)
            return;

        string actorName = GetActorName(context);
        string durationText = FormatSeconds(duration);
        foreach (var target in targets)
        {
            apply(target);
            NotifyTarget(context, target, targetKey, actorName, durationText);
        }

        Reply(context, senderKey, FormatPlayerList(targets), durationText);
    }

    private void ApplyValuedItemCommand(ICommandContext context, string commandName, string syntax, Func<IPlayer, bool> canApply, string invalidStateKey, string senderKey, string targetKey, string valueText, Action<IPlayer> apply)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, syntax, 1))
            return;

        var targets = FindLiveTargetPlayers(context, context.Args[0], canApply, invalidStateKey);
        if (targets == null)
            return;

        string actorName = GetActorName(context);
        foreach (var target in targets)
        {
            apply(target);
            NotifyTarget(context, target, targetKey, actorName, valueText);
        }

        Reply(context, senderKey, FormatPlayerList(targets), valueText);
    }
}
