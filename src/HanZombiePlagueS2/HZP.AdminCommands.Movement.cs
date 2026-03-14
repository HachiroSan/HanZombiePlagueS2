using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void RespawnCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, RespawnCommandName, "<player>", 1))
            return;

        var targets = FindTargetPlayers(context, context.Args[0]);
        if (targets == null)
            return;

        foreach (var target in targets)
        {
            target.Respawn();
            NotifyTarget(context, target, "AdminCommandRespawnTarget", GetActorName(context));
        }

        Reply(context, "AdminCommandRespawnSender", FormatPlayerList(targets));
    }

    private void BringCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, BringCommandName, "<player>", 1))
            return;

        if (!TryGetAliveSender(context, "AdminCommandBringSelfDead", out var sender, out var senderPawn))
            return;

        var targets = FindTargetPlayers(context, context.Args[0])
            ?.Where(player => player.PlayerID != sender.PlayerID && IsAlivePawn(player.PlayerPawn))
            .ToList();

        if (targets == null || targets.Count == 0)
        {
            Reply(context, "AdminCommandBringNoValidTargets");
            return;
        }

        var origin = senderPawn.AbsOrigin;
        var rotation = senderPawn.AbsRotation;
        if (origin == null || rotation == null)
        {
            Reply(context, "AdminCommandBringNoValidTargets");
            return;
        }

        rotation.Value.ToDirectionVectors(out var forward, out _, out _);
        var safeOrigin = new Vector(origin.Value.X + (forward.X * 100f), origin.Value.Y + (forward.Y * 100f), origin.Value.Z);

        foreach (var target in targets)
        {
            target.Teleport(safeOrigin);
            NotifyTarget(context, target, "AdminCommandBringTarget", GetActorName(context));
        }

        Reply(context, "AdminCommandBringSender", FormatPlayerList(targets));
    }

    private void GotoCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, GotoCommandName, "<player>", 1))
            return;

        if (!TryGetAliveSender(context, "AdminCommandGotoSelfDead", out var sender, out _))
            return;

        var target = FindTargetPlayers(context, context.Args[0])?.FirstOrDefault(player => player.PlayerID != sender.PlayerID);
        if (target == null)
        {
            Reply(context, "AdminCommandGotoNoValidTargets");
            return;
        }

        if (!TryGetTargetPosition(context, target, "AdminCommandGotoTargetDead", "AdminCommandGotoNoValidTargets", out var origin, out var rotation))
            return;

        rotation.ToDirectionVectors(out var forward, out _, out _);
        var safeOrigin = new Vector(origin.X - (forward.X * 100f), origin.Y - (forward.Y * 100f), origin.Z);

        sender.Teleport(safeOrigin);
        Reply(context, "AdminCommandGotoSuccess", GetPlayerName(target));
    }

    private void CleanCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        var entities = core.EntitySystem.GetAllEntitiesByClass<CBaseEntity>().ToList();
        int removed = 0;

        foreach (var entity in entities)
        {
            var designerName = entity.DesignerName ?? string.Empty;
            if (designerName.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
                && !designerName.Equals("weapon_c4", StringComparison.OrdinalIgnoreCase)
                && !entity.OwnerEntity.IsValid)
            {
                entity.Despawn();
                removed++;
            }
        }

        Reply(context, "AdminCommandCleanSuccess", removed);
    }

    private void CSayCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, CSayCommandName, "<message>", 1))
            return;

        string message = string.Join(" ", context.Args).Trim();
        if (message.Length == 0)
        {
            ReplySyntax(context, CSayCommandName, "<message>");
            return;
        }

        string display = $"{GetActorName(context)}: {message}";
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (player == null || !player.IsValid || player.IsFakeClient)
                continue;

            player.SendCenter(display);
        }

        Reply(context, "AdminCommandCSaySent");
    }
}
