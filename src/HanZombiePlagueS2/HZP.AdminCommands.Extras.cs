using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void SlayCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, SlayCommandName, "<player>", 1) || string.IsNullOrWhiteSpace(context.Args[0]))
        {
            ReplySyntax(context, SlayCommandName, "<player>");
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0]);
        if (targets == null)
            return;

        foreach (var target in targets)
        {
            var pawn = target.PlayerPawn;
            if (IsAlivePawn(pawn))
            {
                pawn!.CommitSuicide(false, false);
            }
            NotifyTarget(context, target, "AdminCommandSlayTarget", GetActorName(context));
        }

        Reply(context, "AdminCommandSlaySender", FormatPlayerList(targets));
    }

    private void SlapCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, SlapCommandName, "<player> [damage]", 1) || string.IsNullOrWhiteSpace(context.Args[0]))
        {
            ReplySyntax(context, SlapCommandName, "<player> [damage]");
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0]);
        if (targets == null)
            return;

        int damage = 0;
        if (context.Args.Length >= 2 && !TryParseInt(context, context.Args[1], SlapCommandName, "<player> [damage]", 0, 1000, out damage))
            return;

        foreach (var target in targets)
        {
            var pawn = target.PlayerPawn;
            if (!IsAlivePawn(pawn))
                continue;

            pawn!.Health = Math.Max(pawn.Health - damage, 0);
            pawn.HealthUpdated();

            if (pawn.Health == 0)
            {
                pawn.CommitSuicide(false, false);
            }
            else
            {
                var velocity = new Vector(
                    (float)Random.Shared.NextInt64(50, 230) * (Random.Shared.NextDouble() < 0.5 ? -1 : 1),
                    (float)Random.Shared.NextInt64(50, 230) * (Random.Shared.NextDouble() < 0.5 ? -1 : 1),
                    Random.Shared.NextInt64(100, 300)
                );

                target.Teleport(null, null, velocity);
            }

            NotifyTarget(context, target, "AdminCommandSlapTarget", GetActorName(context));
        }

        Reply(context, "AdminCommandSlapSender", FormatPlayerList(targets));
    }
}
