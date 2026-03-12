namespace HanZombiePlagueS2;

public class HZPEconomyCFG
{
    public bool Enable { get; set; } = true;
    public string CreditsCommand { get; set; } = "sw_credits";
    public bool DisableNativeBuy { get; set; } = true;
    public int NativeStartMoney { get; set; } = 0;
    public int NativeMaxMoney { get; set; } = 0;
    public int InfectionReward { get; set; } = 2;
    public int HumanWinReward { get; set; } = 3;
    public int ZombieWinReward { get; set; } = 2;
    public int ParticipationReward { get; set; } = 1;
}
