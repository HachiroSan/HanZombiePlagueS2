using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPPermissionService(ISwiftlyCore core)
{
    public bool HasAnyPermission(IPlayer player, string? permissions)
    {
        if (player == null || !player.IsValid)
        {
            return false;
        }

        ulong steamId = player.SteamID;
        if (steamId == 0)
        {
            return false;
        }

        foreach (string permission in ParsePermissions(permissions))
        {
            if (core.Permission.PlayerHasPermission(steamId, permission))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasPermission(IPlayer player, string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return HasAnyPermission(player, permission);
    }

    public static IReadOnlyList<string> ParsePermissions(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
        {
            return [];
        }

        return permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(permission => permission.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
