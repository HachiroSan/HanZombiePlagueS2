using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPEconomyService(
    HZPDatabaseService databaseService,
    HZPEconomyState state,
    IOptionsMonitor<HZPEconomyCFG> economyCFG,
    ILogger<HZPEconomyService> logger)
{
    public void LoadPlayer(IPlayer? player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return;
        }

        _ = LoadPlayerAsync(player.SteamID);
    }

    public void ClearPlayer(IPlayer? player)
    {
        if (player == null || !player.IsValid || player.SteamID == 0)
        {
            return;
        }

        state.ClearBalance(player.SteamID);
    }

    public int GetBalance(ulong steamId)
    {
        return state.GetBalance(steamId);
    }

    public bool IsLoaded(ulong steamId)
    {
        return state.IsLoaded(steamId);
    }

    public Task<int> EnsureLoadedAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        if (state.IsLoaded(steamId))
        {
            return Task.FromResult(state.GetBalance(steamId));
        }

        return LoadPlayerAsync(steamId, cancellationToken);
    }

    public async Task<int> LoadPlayerAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        if (steamId == 0)
        {
            return 0;
        }

        try
        {
            var currency = await databaseService.GetPlayerCurrencyAsync(steamId, cancellationToken);
            state.SetBalance(steamId, currency.Balance);
            return currency.Balance;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load currency for SteamID {SteamId}.", steamId);
            return state.GetBalance(steamId);
        }
    }

    public async Task<bool> AddCurrencyAsync(ulong steamId, int amount, string reason, CancellationToken cancellationToken = default)
    {
        if (!economyCFG.CurrentValue.Enable || steamId == 0 || amount <= 0)
        {
            return false;
        }

        try
        {
            await databaseService.AddCurrencyAsync(steamId, amount, reason, cancellationToken);
            state.AddBalance(steamId, amount);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add currency for SteamID {SteamId}.", steamId);
            return false;
        }
    }

    public async Task<bool> TrySpendCurrencyAsync(ulong steamId, int amount, string reason, CancellationToken cancellationToken = default)
    {
        if (!economyCFG.CurrentValue.Enable || steamId == 0)
        {
            return false;
        }

        if (amount <= 0)
        {
            return true;
        }

        try
        {
            bool success = await databaseService.TrySpendCurrencyAsync(steamId, amount, reason, cancellationToken);
            if (success)
            {
                state.AddBalance(steamId, -amount);
            }
            else
            {
                await LoadPlayerAsync(steamId, cancellationToken);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to spend currency for SteamID {SteamId}.", steamId);
            return false;
        }
    }
}
