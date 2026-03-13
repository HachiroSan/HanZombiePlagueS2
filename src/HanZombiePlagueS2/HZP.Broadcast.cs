using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPBroadcastService(
    ISwiftlyCore core,
    HZPHelpers helpers,
    HZPBroadcastCountryService countryService,
    HZPMapVoteService mapVoteService,
    HZPBroadcastState state,
    IOptionsMonitor<HZPBroadcastCFG> broadcastCFG,
    ILogger<HZPBroadcastService> logger)
{
    private CancellationTokenSource? _adTimer;

    public void Start()
    {
        Stop();

        var cfg = broadcastCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableAds || cfg.AdInterval <= 0 || cfg.Ads.Count == 0)
        {
            return;
        }

        _adTimer = core.Scheduler.RepeatBySeconds(cfg.AdInterval, BroadcastNextAd);
    }

    public void Stop()
    {
        _adTimer?.Cancel();
        _adTimer = null;
    }

    public void QueueWelcome(IPlayer? player)
    {
        var cfg = broadcastCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableWelcome || player == null || !player.IsValid || player.IsFakeClient)
        {
            return;
        }

        state.PendingWelcomePlayers[player.Slot] = new HZPWelcomePendingPlayer
        {
            Slot = player.Slot,
            SteamId = player.SteamID,
            DueTimeUtc = DateTime.UtcNow.AddSeconds(cfg.WelcomeDelay <= 0 ? 1 : cfg.WelcomeDelay)
        };
        state.WelcomedSlots.Remove(player.Slot);
    }

    public void OnDisconnect(int slot)
    {
        state.ClearPlayer(slot);
    }

    public void ProcessPendingWelcomes()
    {
        var cfg = broadcastCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableWelcome || state.PendingWelcomePlayers.Count == 0)
        {
            return;
        }

        var dueSlots = state.PendingWelcomePlayers
            .Where(kvp => kvp.Value.DueTimeUtc <= DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var slot in dueSlots)
        {
            if (!state.PendingWelcomePlayers.Remove(slot, out var pending))
            {
                continue;
            }

            var player = core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p != null && p.IsValid && !p.IsFakeClient && p.Slot == pending.Slot && p.SteamID == pending.SteamId);
            if (player == null)
            {
                continue;
            }

            if (state.WelcomedSlots.Contains(slot))
            {
                continue;
            }

            _ = SendWelcomeAsync(player);
            state.WelcomedSlots.Add(slot);
        }
    }

    private async Task SendWelcomeAsync(IPlayer player)
    {
        try
        {
            string countryName = string.Empty;
            if (broadcastCFG.CurrentValue.EnableCountryAnnounce)
            {
                countryName = await countryService.GetCountryDisplayNameAsync(player, broadcastCFG.CurrentValue.AnnounceUnknownCountry);
            }

            foreach (var message in broadcastCFG.CurrentValue.WelcomeMessages)
            {
                if (string.IsNullOrWhiteSpace(message.Message))
                {
                    continue;
                }

                if (message.Message.Contains("{COUNTRY}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(countryName))
                {
                    continue;
                }

                string rendered = ReplacePlaceholders(message.Message, player, countryName);
                SendMessage(player, rendered, message.Type, broadcast: message.Message.Contains("{COUNTRY}", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Broadcast] Failed to send welcome for {SteamId}", player.SteamID);
        }
    }

    private void BroadcastNextAd()
    {
        var cfg = broadcastCFG.CurrentValue;
        if (!cfg.Enable || !cfg.EnableAds || cfg.Ads.Count == 0)
        {
            return;
        }

        var ad = cfg.Ads[state.CurrentAdIndex % cfg.Ads.Count];
        state.CurrentAdIndex++;
        if (string.IsNullOrWhiteSpace(ad.Message))
        {
            return;
        }

        string rendered = ReplacePlaceholders(ad.Message, null, string.Empty);
        SendMessage(null, rendered, ad.Type, broadcast: true);
    }

    private string ReplacePlaceholders(string message, IPlayer? player, string countryName)
    {
        int playerCount = core.PlayerManager.GetAllPlayers().Count(p => p != null && p.IsValid && !p.IsFakeClient);
        string coloredPlayer = player == null || !player.IsValid
            ? "Player"
            : $"[lightblue]{player.Name}[olive]";
        string coloredCountry = string.IsNullOrWhiteSpace(countryName) ? string.Empty : $"[gold]{countryName}[olive]";
        string nextMap = GetNextMapValue();
        string coloredNextMap = string.IsNullOrWhiteSpace(nextMap) ? string.Empty : $"[gold]{nextMap}[olive]";
        return message
            .Replace("{PLAYER}", coloredPlayer, StringComparison.OrdinalIgnoreCase)
            .Replace("{COUNTRY}", coloredCountry, StringComparison.OrdinalIgnoreCase)
            .Replace("{TIME}", DateTime.Now.ToString("HH:mm:ss"), StringComparison.OrdinalIgnoreCase)
            .Replace("{DATE}", DateTime.Now.ToString("dd.MM.yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{PLAYERS}", playerCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{MAXPLAYERS}", "32", StringComparison.OrdinalIgnoreCase)
            .Replace("{NEXTMAP}", coloredNextMap, StringComparison.OrdinalIgnoreCase);
    }

    private string GetNextMapValue()
    {
        return mapVoteService.GetNextMapDisplayName() ?? string.Empty;
    }

    private void SendMessage(IPlayer? player, string message, HZPBroadcastMessageType type, bool broadcast)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (broadcast)
        {
            foreach (var target in core.PlayerManager.GetAllPlayers())
            {
                if (target == null || !target.IsValid || target.IsFakeClient)
                {
                    continue;
                }

                SendToPlayer(target, message, type);
            }
            return;
        }

        if (player != null && player.IsValid)
        {
            SendToPlayer(player, message, type);
        }
    }

    private void SendToPlayer(IPlayer player, string message, HZPBroadcastMessageType type)
    {
        switch (type)
        {
            case HZPBroadcastMessageType.Center:
                player.SendCenter(message);
                break;
            default:
                helpers.SendChatRaw(player, message);
                break;
        }
    }
}
