using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands(
    ISwiftlyCore core,
    HZPHelpers helpers,
    HZPPermissionService permissionService,
    IHanZombiePlagueAPI api,
    HZPBanService banService,
    HZPEconomyService economyService,
    IOptionsMonitor<HZPMainCFG> mainCFG,
    IOptionsMonitor<HZPStoreCFG> storeCFG)
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
    private const string CashAddCommandName = "hzp_cash_add";
    private const string CashSetCommandName = "hzp_cash_set";
    private const string TVaccineCommandName = "hzp_tvaccine";
    private const string ScbaCommandName = "hzp_scba";
    private const string GodCommandName = "hzp_god";
    private const string InfiniteAmmoCommandName = "hzp_infiniteammo";
    private const string TVirusGrenadeCommandName = "hzp_tvirusgrenade";
    private const string AddHealthCommandName = "hzp_addhealth";
    private const string HumanWinCommandName = "hzp_humanwin";
    private const string ZombieWinCommandName = "hzp_zombiewin";
    private const string CheckRoundCommandName = "hzp_checkround";
    private const string RestartRoundCommandName = "hzp_restartround";
    private const string BanCommandName = "hzp_ban";
    private const string GlobalBanCommandName = "hzp_globalban";
    private const string BanIpCommandName = "hzp_banip";
    private const string GlobalBanIpCommandName = "hzp_globalbanip";
    private const string UnbanCommandName = "hzp_unban";
    private const string GlobalUnbanCommandName = "hzp_globalunban";
    private const string UnbanIpCommandName = "hzp_unbanip";
    private const string GlobalUnbanIpCommandName = "hzp_globalunbanip";

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
        core.Command.RegisterCommand(CashAddCommandName, CashAddCommand);
        core.Command.RegisterCommand(CashSetCommandName, CashSetCommand);
        core.Command.RegisterCommand(TVaccineCommandName, TVaccineCommand);
        core.Command.RegisterCommand(ScbaCommandName, ScbaCommand);
        core.Command.RegisterCommand(GodCommandName, GodCommand);
        core.Command.RegisterCommand(InfiniteAmmoCommandName, InfiniteAmmoCommand);
        core.Command.RegisterCommand(TVirusGrenadeCommandName, TVirusGrenadeCommand);
        core.Command.RegisterCommand(AddHealthCommandName, AddHealthCommand);
        core.Command.RegisterCommand(HumanWinCommandName, HumanWinCommand);
        core.Command.RegisterCommand(ZombieWinCommandName, ZombieWinCommand);
        core.Command.RegisterCommand(CheckRoundCommandName, CheckRoundCommand);
        core.Command.RegisterCommand(RestartRoundCommandName, RestartRoundCommand);
        core.Command.RegisterCommand(BanCommandName, BanCommand);
        core.Command.RegisterCommand(GlobalBanCommandName, GlobalBanCommand);
        core.Command.RegisterCommand(BanIpCommandName, BanIpCommand);
        core.Command.RegisterCommand(GlobalBanIpCommandName, GlobalBanIpCommand);
        core.Command.RegisterCommand(UnbanCommandName, UnbanCommand);
        core.Command.RegisterCommand(GlobalUnbanCommandName, GlobalUnbanCommand);
        core.Command.RegisterCommand(UnbanIpCommandName, UnbanIpCommand);
        core.Command.RegisterCommand(GlobalUnbanIpCommandName, GlobalUnbanIpCommand);
    }
}
