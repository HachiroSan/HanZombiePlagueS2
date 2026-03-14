using SwiftlyS2.Shared.Commands;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void HumanWinCommand(ICommandContext context) => ApplyServerCommand(context, true, "AdminCommandHumanWinSender", () => api.HZP_SetHumanWin());
    private void ZombieWinCommand(ICommandContext context) => ApplyServerCommand(context, true, "AdminCommandZombieWinSender", () => api.HZP_SetZombieWin());
    private void CheckRoundCommand(ICommandContext context) => ApplyServerCommand(context, true, "AdminCommandCheckRoundSender", () => api.HZP_CheckRoundWinConditions());
    private void RestartRoundCommand(ICommandContext context) => ApplyServerCommand(context, false, "AdminCommandRestartRoundSender", () => helpers.restartgame());

    private void ApplyServerCommand(ICommandContext context, bool requireActiveRound, string senderKey, Action apply)
    {
        if (!HasAdminAccess(context))
            return;

        if (requireActiveRound && !api.GameStart)
        {
            Reply(context, "AdminCommandRoundNotActive");
            return;
        }

        apply();
        Reply(context, senderKey);
    }
}
