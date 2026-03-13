namespace HanZombiePlagueS2;

public enum HZPBroadcastMessageType
{
    Chat,
    Center
}

public sealed class HZPBroadcastMessage
{
    public HZPBroadcastMessageType Type { get; set; } = HZPBroadcastMessageType.Chat;
    public string Message { get; set; } = string.Empty;
}

public sealed class HZPBroadcastCFG
{
    public bool Enable { get; set; } = true;
    public bool EnableAds { get; set; } = true;
    public bool EnableWelcome { get; set; } = true;
    public bool EnableCountryAnnounce { get; set; } = true;
    public bool AnnounceUnknownCountry { get; set; } = false;
    public string UnknownCountryLabel { get; set; } = "Unknown";
    public string DebugCountryCodeOverride { get; set; } = string.Empty;
    public int CacheExpiryHours { get; set; } = 168;
    public float WelcomeDelay { get; set; } = 3f;
    public float AdInterval { get; set; } = 45f;
    public List<HZPBroadcastMessage> WelcomeMessages { get; set; } =
    [
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Welcome to the outbreak, {PLAYER}." },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "{PLAYER} arrived from {COUNTRY}." }
    ];
    public List<HZPBroadcastMessage> Ads { get; set; } =
    [
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use [gold]!loadout[olive] before the round starts." },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use [gold]!store[olive] to spend your [gold]cash[olive] on extra gear." },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use [gold]!nextmap[olive], [gold]!rtv[olive], and [gold]!nominate[olive] to help decide the next battlefield." }
    ];
}
