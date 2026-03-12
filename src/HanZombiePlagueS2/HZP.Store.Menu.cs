using System.Drawing;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public class HZPStoreMenu(
    ISwiftlyCore core,
    HZPMenuHelper menuHelper,
    HZPHelpers helpers,
    HZPStoreService storeService,
    HZPEconomyService economyService,
    HZPGlobals globals)
{
    public IMenuAPI? OpenStoreMenu(IPlayer player)
    {
        if (!storeService.CanOpenStore(player, out var denyKey))
        {
            if (player != null && player.IsValid && !string.IsNullOrWhiteSpace(denyKey))
            {
                helpers.SendChatT(player, denyKey);
            }

            return null;
        }

        var items = storeService.GetAvailableItemsForPlayer(player).ToList();
        if (items.Count == 0)
        {
            helpers.SendChatT(player, "StoreEmpty");
            return null;
        }

        globals.IsZombie.TryGetValue(player.PlayerID, out bool isZombie);
        string titleKey = isZombie ? "StoreTitleZombie" : "StoreTitleHuman";
        IMenuAPI menu = menuHelper.CreateMenu(helpers.T(player, titleKey));
        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            helpers.T(player, "StoreBalance", storeService.GetBalance(player)),
            Color.Gold, Color.LightGreen, Color.Gold),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        foreach (var item in items)
        {
            string label = $"{item.DisplayName} [{item.Price}]";
            var button = new ButtonMenuOption(label)
            {
                TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
                CloseAfterClick = true,
                Tag = "extend"
            };

            button.Click += async (_, args) =>
            {
                var clicker = args.Player;
                core.Scheduler.NextTick(() =>
                {
                    if (clicker == null || !clicker.IsValid)
                    {
                        return;
                    }

                    _ = HandlePurchaseAsync(clicker, item);
                });
            };

            menu.AddOption(button);
        }

        core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }

    public async Task OpenStoreMenuAsync(IPlayer player)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        await economyService.EnsureLoadedAsync(player.SteamID);
        if (player == null || !player.IsValid)
        {
            return;
        }

        OpenStoreMenu(player);
    }

    private async Task HandlePurchaseAsync(IPlayer player, HZPStoreItemEntry item)
    {
        var result = await storeService.TryPurchaseAsync(player, item);
        if (!result.Success)
        {
            helpers.SendChatT(player, result.MessageKey);
            return;
        }

        helpers.SendChatT(player, "StorePurchaseSuccessDetail", item.DisplayName, item.Price, storeService.GetBalance(player));
    }
}
