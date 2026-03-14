using System.Text.RegularExpressions;

namespace HanZombiePlagueS2;

internal static partial class HZPBanDurationParser
{
    [GeneratedRegex("(\\d+)(mo|[smhdwy])", RegexOptions.IgnoreCase)]
    private static partial Regex DurationPartRegex();

    public static bool TryParse(string input, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string value = input.Trim().ToLowerInvariant();
        if (value is "0" or "perm" or "permanent" or "never")
        {
            duration = TimeSpan.Zero;
            return true;
        }

        var matches = DurationPartRegex().Matches(value);
        if (matches.Count == 0)
        {
            return false;
        }

        string combined = string.Concat(matches.Select(match => match.Value));
        if (!string.Equals(combined, value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        long totalSeconds = 0;
        foreach (Match match in matches)
        {
            if (!long.TryParse(match.Groups[1].Value, out long amount))
            {
                return false;
            }

            totalSeconds += match.Groups[2].Value.ToLowerInvariant() switch
            {
                "s" => amount,
                "m" => amount * 60,
                "h" => amount * 3600,
                "d" => amount * 86400,
                "w" => amount * 604800,
                "mo" => amount * 2592000,
                "y" => amount * 31536000,
                _ => 0
            };
        }

        if (totalSeconds <= 0)
        {
            return false;
        }

        duration = TimeSpan.FromSeconds(totalSeconds);
        return true;
    }
}
