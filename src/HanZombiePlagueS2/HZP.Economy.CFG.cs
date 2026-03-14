namespace HanZombiePlagueS2;

public class HZPEconomyCFG
{
    public bool Enable { get; set; } = true;
    public string CashCommand { get; set; } = "sw_cash";

    public string CreditsCommand
    {
        get => CashCommand;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                CashCommand = value;
        }
    }

    public bool DisableNativeBuy { get; set; } = true;
    public int NativeStartMoney { get; set; } = 0;
    public int NativeMaxMoney { get; set; } = 0;
    public int InfectionReward { get; set; } = 2;
    public int HumanKillZombieReward { get; set; } = 1;
    public int ZombieKillHumanReward { get; set; } = 1;
    public int HumanWinReward { get; set; } = 3;
    public int ZombieWinReward { get; set; } = 2;
    public int ParticipationReward { get; set; } = 1;
}
