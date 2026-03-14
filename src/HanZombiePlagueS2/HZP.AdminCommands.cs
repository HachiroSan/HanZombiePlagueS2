using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

public sealed class HZPAdminCommands(
    ISwiftlyCore core,
    HZPHelpers helpers,
    HZPPermissionService permissionService,
    IHanZombiePlagueAPI api,
    IOptionsMonitor<HZPMainCFG> mainCFG)
{
    private const string RespawnCommandName = "hzp_respawn";
    private const string BringCommandName = "hzp_bring";
    private const string GotoCommandName = "hzp_goto";
    private const string CleanCommandName = "hzp_clean";
    private const string CSayCommandName = "hzp_csay";
    private const string HumanCommandName = "hzp_human";
    private const string ZombieCommandName = "hzp_zombie";
    private const string InfectCommandName = "hzp_infect";
    private const string MotherCommandName = "hzp_mother";
    private const string NemesisCommandName = "hzp_nemesis";
    private const string AssassinCommandName = "hzp_assassin";
    private const string HeroCommandName = "hzp_hero";
    private const string SurvivorCommandName = "hzp_survivor";
    private const string SniperCommandName = "hzp_sniper";

    public void RegisterCommands()
    {
        core.Command.RegisterCommand(RespawnCommandName, RespawnCommand);
        core.Command.RegisterCommand(BringCommandName, BringCommand);
        core.Command.RegisterCommand(GotoCommandName, GotoCommand);
        core.Command.RegisterCommand(CleanCommandName, CleanCommand);
        core.Command.RegisterCommand(CSayCommandName, CSayCommand);
        core.Command.RegisterCommand(HumanCommandName, HumanCommand);
        core.Command.RegisterCommand(ZombieCommandName, ZombieCommand);
        core.Command.RegisterCommand(InfectCommandName, InfectCommand);
        core.Command.RegisterCommand(MotherCommandName, MotherCommand);
        core.Command.RegisterCommand(NemesisCommandName, NemesisCommand);
        core.Command.RegisterCommand(AssassinCommandName, AssassinCommand);
        core.Command.RegisterCommand(HeroCommandName, HeroCommand);
        core.Command.RegisterCommand(SurvivorCommandName, SurvivorCommand);
        core.Command.RegisterCommand(SniperCommandName, SniperCommand);
    }

    private void HumanCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            HumanCommandName,
            "AdminRoleHuman",
            target => api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsZombie",
            target => api.HZP_SetTargetHuman(target));
    }

    private void ZombieCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            ZombieCommandName,
            "AdminRoleZombie",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetZombie(target));
    }

    private void InfectCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            InfectCommandName,
            "AdminRoleInfected",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_InfectPlayer(target, true));
    }

    private void MotherCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            MotherCommandName,
            "AdminRoleMother",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_InfectMotherZombie(target));
    }

    private void NemesisCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            NemesisCommandName,
            "AdminRoleNemesis",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetNemesis(target));
    }

    private void AssassinCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            AssassinCommandName,
            "AdminRoleAssassin",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetAssassin(target));
    }

    private void HeroCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            HeroCommandName,
            "AdminRoleHero",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetHero(target));
    }

    private void SurvivorCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            SurvivorCommandName,
            "AdminRoleSurvivor",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetSurvivor(target));
    }

    private void SniperCommand(ICommandContext context)
    {
        ApplyRoleCommand(
            context,
            SniperCommandName,
            "AdminRoleSniper",
            target => !api.HZP_IsZombie(target.PlayerID),
            "AdminCommandRoleNeedsHuman",
            target => api.HZP_SetTargetSniper(target));
    }

    private void RespawnCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
        {
            return;
        }

        if (!RequirePlayerSender(context) || !RequireArgs(context, RespawnCommandName, "<player>", 1))
        {
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0]);
        if (targets == null)
        {
            return;
        }

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
        {
            return;
        }

        if (!RequirePlayerSender(context) || !RequireArgs(context, BringCommandName, "<player>", 1))
        {
            return;
        }

        var sender = context.Sender!;
        var senderPawn = sender.PlayerPawn;
        if (!IsAlivePawn(senderPawn))
        {
            Reply(context, "AdminCommandBringSelfDead");
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0])
            ?.Where(player => player.PlayerID != sender.PlayerID && IsAlivePawn(player.PlayerPawn))
            .ToList();

        if (targets == null || targets.Count == 0)
        {
            Reply(context, "AdminCommandBringNoValidTargets");
            return;
        }

        var origin = senderPawn!.AbsOrigin;
        var rotation = senderPawn.AbsRotation;
        if (origin == null || rotation == null)
        {
            Reply(context, "AdminCommandBringNoValidTargets");
            return;
        }

        rotation.Value.ToDirectionVectors(out var forward, out _, out _);
        var safeOrigin = new Vector(
            origin.Value.X + (forward.X * 100f),
            origin.Value.Y + (forward.Y * 100f),
            origin.Value.Z);

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
        {
            return;
        }

        if (!RequirePlayerSender(context) || !RequireArgs(context, GotoCommandName, "<player>", 1))
        {
            return;
        }

        var sender = context.Sender!;
        var senderPawn = sender.PlayerPawn;
        if (!IsAlivePawn(senderPawn))
        {
            Reply(context, "AdminCommandGotoSelfDead");
            return;
        }

        var target = FindTargetPlayers(context, context.Args[0])
            ?.FirstOrDefault(player => player.PlayerID != sender.PlayerID);

        if (target == null)
        {
            Reply(context, "AdminCommandGotoNoValidTargets");
            return;
        }

        var targetPawn = target.PlayerPawn;
        if (!IsAlivePawn(targetPawn))
        {
            Reply(context, "AdminCommandGotoTargetDead");
            return;
        }

        var origin = targetPawn!.AbsOrigin;
        var rotation = targetPawn.AbsRotation;
        if (origin == null || rotation == null)
        {
            Reply(context, "AdminCommandGotoNoValidTargets");
            return;
        }

        rotation.Value.ToDirectionVectors(out var forward, out _, out _);
        var safeOrigin = new Vector(
            origin.Value.X - (forward.X * 100f),
            origin.Value.Y - (forward.Y * 100f),
            origin.Value.Z);

        sender.Teleport(safeOrigin);
        Reply(context, "AdminCommandGotoSuccess", GetPlayerName(target));
    }

    private void CleanCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
        {
            return;
        }

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
        {
            return;
        }

        if (!RequireArgs(context, CSayCommandName, "<message>", 1))
        {
            return;
        }

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
            {
                continue;
            }

            player.SendCenter(display);
        }

        Reply(context, "AdminCommandCSaySent");
    }

    private void ApplyRoleCommand(
        ICommandContext context,
        string commandName,
        string roleKey,
        Func<IPlayer, bool> canApply,
        string invalidStateKey,
        Action<IPlayer> apply)
    {
        if (!HasAdminAccess(context))
        {
            return;
        }

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, "<player>", 1))
        {
            return;
        }

        var targets = FindTargetPlayers(context, context.Args[0])
            ?.Where(player => IsAlivePawn(player.PlayerPawn))
            .Where(canApply)
            .ToList();

        if (targets == null || targets.Count == 0)
        {
            Reply(context, invalidStateKey);
            return;
        }

        string roleName = LocalizeRole(context, roleKey);
        string actorName = GetActorName(context);

        foreach (var target in targets)
        {
            apply(target);
            NotifyTarget(context, target, "AdminCommandRoleTarget", actorName, roleName);
        }

        Reply(context, "AdminCommandRoleSender", FormatPlayerList(targets), roleName);
    }

    private bool HasAdminAccess(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            return true;
        }

        string permissions = mainCFG.CurrentValue.AdminMenuPermission;
        if (string.IsNullOrWhiteSpace(permissions) || permissionService.HasAnyPermission(context.Sender!, permissions))
        {
            return true;
        }

        helpers.SendChatT(context.Sender, "NoPermission");
        return false;
    }

    private bool RequirePlayerSender(ICommandContext context)
    {
        if (context.IsSentByPlayer)
        {
            return true;
        }

        Reply(context, "AdminCommandPlayerOnly");
        return false;
    }

    private bool RequireArgs(ICommandContext context, string commandName, string syntax, int requiredArgs)
    {
        if (context.Args.Length >= requiredArgs)
        {
            return true;
        }

        ReplySyntax(context, commandName, syntax);
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
        {
            return;
        }

        helpers.SendChatT(target, key, args);
    }

    private static bool IsAlivePawn(CCSPlayerPawn? pawn)
    {
        return pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    private string GetActorName(ICommandContext context)
    {
        if (!context.IsSentByPlayer || context.Sender?.Controller == null || !context.Sender.Controller.IsValid)
        {
            return "Console";
        }

        return context.Sender.Controller.PlayerName;
    }

    private string LocalizeRole(ICommandContext context, string key)
    {
        if (context.IsSentByPlayer)
        {
            return helpers.T(context.Sender, key);
        }

        return core.Localizer[key];
    }

    private static string GetPlayerName(IPlayer player)
    {
        if (player.Controller != null && player.Controller.IsValid)
        {
            return player.Controller.PlayerName;
        }

        return player.Name;
    }

    private static string FormatPlayerList(IEnumerable<IPlayer> players)
    {
        return string.Join(", ", players.Select(GetPlayerName));
    }
}
