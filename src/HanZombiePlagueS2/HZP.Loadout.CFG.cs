namespace HanZombiePlagueS2;

public class HZPLoadoutEntry
{
    public string Id { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
    public string WeaponCommand { get; set; } = string.Empty;
    public string NativeWeaponClassName { get; set; } = string.Empty;
    public int NativeWeaponSlot { get; set; } = 0;
    public string AllowedModes { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
}

public class HZPLoadoutCFG
{
    public bool Enable { get; set; } = true;
    public string LoadoutCommand { get; set; } = "sw_loadout";
    public bool AutoOpenOnSpawnBeforeRoundStart { get; set; } = true;
    public bool AllowOnlyAliveHuman { get; set; } = true;
    public bool AllowDuringPrep { get; set; } = true;
    public bool AllowAfterGameStart { get; set; } = false;
    public bool DenySpecialHumans { get; set; } = true;
    public List<HZPLoadoutEntry> PrimaryWeapons { get; set; } = [];
    public List<HZPLoadoutEntry> SecondaryWeapons { get; set; } = [];
}
