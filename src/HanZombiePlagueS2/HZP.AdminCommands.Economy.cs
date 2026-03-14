using SwiftlyS2.Shared.Commands;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private async void CashAddCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, CashAddCommandName, "<player> <amount>", 2))
            return;

        if (!TryParseInt(context, context.Args[1], CashAddCommandName, "<player> <amount>", 1, int.MaxValue, out int amount))
            return;

        var targets = FindEconomyTargetPlayers(context, context.Args[0]);
        if (targets == null)
            return;

        string actorName = GetActorName(context);

        foreach (var target in targets)
        {
            bool success = await economyService.AddCurrencyAsync(target.SteamID, amount, $"admin_add:{actorName}");
            if (!success)
            {
                Reply(context, "AdminCommandCashFailed", GetPlayerName(target));
                continue;
            }

            int balance = await economyService.EnsureLoadedAsync(target.SteamID);
            NotifyTarget(context, target, "AdminCommandCashAddTarget", actorName, helpers.FormatCurrency(amount), helpers.FormatCurrency(balance));
        }

        Reply(context, "AdminCommandCashAddSender", FormatPlayerList(targets), helpers.FormatCurrency(amount));
    }

    private async void CashSetCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, CashSetCommandName, "<player> <amount>", 2))
            return;

        if (!TryParseInt(context, context.Args[1], CashSetCommandName, "<player> <amount>", 0, int.MaxValue, out int targetBalance))
            return;

        var targets = FindEconomyTargetPlayers(context, context.Args[0]);
        if (targets == null)
            return;

        string actorName = GetActorName(context);

        foreach (var target in targets)
        {
            int currentBalance = await economyService.EnsureLoadedAsync(target.SteamID);
            int delta = targetBalance - currentBalance;
            bool success = delta switch
            {
                > 0 => await economyService.AddCurrencyAsync(target.SteamID, delta, $"admin_set_add:{actorName}"),
                < 0 => await economyService.TrySpendCurrencyAsync(target.SteamID, -delta, $"admin_set_remove:{actorName}"),
                _ => true
            };

            if (!success)
            {
                Reply(context, "AdminCommandCashFailed", GetPlayerName(target));
                continue;
            }

            int balance = await economyService.EnsureLoadedAsync(target.SteamID);
            NotifyTarget(context, target, "AdminCommandCashSetTarget", actorName, helpers.FormatCurrency(balance));
        }

        Reply(context, "AdminCommandCashSetSender", FormatPlayerList(targets), helpers.FormatCurrency(targetBalance));
    }
}
