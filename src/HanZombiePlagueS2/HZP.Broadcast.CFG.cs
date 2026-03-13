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
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Welcome, {PLAYER}!" },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "{PLAYER} connected from {COUNTRY}" }
    ];
    public List<HZPBroadcastMessage> Ads { get; set; } =
    [
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use !loadout before the round starts." },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use !store to buy extra items with credits." },
        new() { Type = HZPBroadcastMessageType.Chat, Message = "Use !nextmap, !rtv and !nominate to help pick the next map." }
    ];
}
