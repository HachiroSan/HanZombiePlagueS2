using SwiftlyS2.Shared.Commands;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void MinPlayersCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, MinPlayersCommandName, "<count>", 1))
            return;

        if (!TryParseInt(context, context.Args[0], MinPlayersCommandName, "<count>", 1, 64, out int minPlayers))
            return;

        globals.RuntimeMinPlayersToStart = minPlayers;
        Reply(context, "AdminCommandMinPlayersSet", minPlayers);
    }

    private void BotQuotaCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        int configured = Math.Max(0, mainCFG.CurrentValue.BotQuota);
        int effective = Math.Max(0, globals.RuntimeBotQuota ?? configured);

        if (context.Args.Length == 0)
        {
            Reply(context, "AdminCommandBotQuotaStatus", effective, configured);
            return;
        }

        if (!TryParseInt(context, context.Args[0], BotQuotaCommandName, "[count]", 0, 64, out int quota))
            return;

        globals.RuntimeBotQuota = quota;
        core.Engine.ExecuteCommand("bot_quota_mode fill");
        core.Engine.ExecuteCommand($"bot_quota {quota}");
        Reply(context, "AdminCommandBotQuotaSet", quota);
    }
}
