namespace HanZombiePlagueS2;

public enum StoreGrantType
{
    TVaccine,
    TVirus,
    AddHealth,
    GodMode,
    InfiniteAmmo,
    ScbaSuit,
    FireGrenade,
    LightGrenade,
    FreezeGrenade,
    TeleportGrenade,
    IncGrenade,
    TVirusGrenade,
    CustomWeapon
}

public class HZPStoreItemEntry
{
    public string Id { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
    public int Price { get; set; } = 0;
    public bool ShowInStore { get; set; } = true;
    public bool ShowInAdminMenu { get; set; } = true;
    public StoreGrantType GrantType { get; set; } = StoreGrantType.AddHealth;
    public int IntValue { get; set; } = 0;
    public float FloatValue { get; set; } = 0;
    public string HiddenCommand { get; set; } = string.Empty;
    public string AllowedModes { get; set; } = string.Empty;
    public bool HumanOnly { get; set; } = false;
    public bool ZombieOnly { get; set; } = false;
    public bool DenySpecialHumans { get; set; } = false;
    public int MaxPerLife { get; set; } = 0;
    public int MaxPerRound { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
}

public class HZPStoreCFG
{
    public bool Enable { get; set; } = true;
    public string StoreCommand { get; set; } = "sw_store";
    public bool AllowDuringPrep { get; set; } = true;
    public bool AllowAfterGameStart { get; set; } = true;
    public bool AliveOnly { get; set; } = true;
    public List<HZPStoreItemEntry> ItemList { get; set; } = [];
}
