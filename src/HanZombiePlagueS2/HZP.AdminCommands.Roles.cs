using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void HumanCommand(ICommandContext context) =>
        ApplyRoleCommand(context, HumanCommandName, "AdminRoleHuman", target => api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsZombie", target => api.HZP_SetTargetHuman(target));

    private void ZombieCommand(ICommandContext context) =>
        ApplyRoleCommand(context, ZombieCommandName, "AdminRoleZombie", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetZombie(target));

    private void InfectCommand(ICommandContext context) =>
        ApplyRoleCommand(context, InfectCommandName, "AdminRoleInfected", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_InfectPlayer(target, true));

    private void MotherCommand(ICommandContext context) =>
        ApplyRoleCommand(context, MotherCommandName, "AdminRoleMother", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_InfectMotherZombie(target));

    private void NemesisCommand(ICommandContext context) =>
        ApplyRoleCommand(context, NemesisCommandName, "AdminRoleNemesis", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetNemesis(target));

    private void AssassinCommand(ICommandContext context) =>
        ApplyRoleCommand(context, AssassinCommandName, "AdminRoleAssassin", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetAssassin(target));

    private void HeroCommand(ICommandContext context) =>
        ApplyRoleCommand(context, HeroCommandName, "AdminRoleHero", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetHero(target));

    private void SurvivorCommand(ICommandContext context) =>
        ApplyRoleCommand(context, SurvivorCommandName, "AdminRoleSurvivor", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetSurvivor(target));

    private void SniperCommand(ICommandContext context) =>
        ApplyRoleCommand(context, SniperCommandName, "AdminRoleSniper", target => !api.HZP_IsZombie(target.PlayerID), "AdminCommandRoleNeedsHuman", target => api.HZP_SetTargetSniper(target));

    private void ApplyRoleCommand(ICommandContext context, string commandName, string roleKey, Func<IPlayer, bool> canApply, string invalidStateKey, Action<IPlayer> apply)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequirePlayerSender(context) || !RequireArgs(context, commandName, "<player>", 1))
            return;

        var targets = FindLiveTargetPlayers(context, context.Args[0], canApply, invalidStateKey);
        if (targets == null)
            return;

        string roleName = LocalizeRole(context, roleKey);
        string actorName = GetActorName(context);
        foreach (var target in targets)
        {
            apply(target);
            NotifyTarget(context, target, "AdminCommandRoleTarget", actorName, roleName);
        }

        Reply(context, "AdminCommandRoleSender", FormatPlayerList(targets), roleName);
    }
}
