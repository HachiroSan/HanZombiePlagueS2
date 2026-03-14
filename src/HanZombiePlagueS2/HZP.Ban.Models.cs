namespace HanZombiePlagueS2;

public enum HZPBanType
{
    SteamId = 1,
    IP = 2
}

public sealed class HZPBanRecord
{
    public long Id { get; set; }
    public long SteamId64 { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerIp { get; set; } = string.Empty;
    public HZPBanType BanType { get; set; }
    public long ExpiresAt { get; set; }
    public long Length { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long AdminSteamId64 { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = string.Empty;
    public bool GlobalBan { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }

    public bool IsPermanent => ExpiresAt == 0;
}

public sealed class HZPBanCreateRequest
{
    public long SteamId64 { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerIp { get; set; } = string.Empty;
    public HZPBanType BanType { get; set; }
    public long ExpiresAt { get; set; }
    public long Length { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long AdminSteamId64 { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = string.Empty;
    public bool GlobalBan { get; set; }
}
