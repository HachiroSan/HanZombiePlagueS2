using System.Drawing;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public class HZPAdminItemMenu(
    ISwiftlyCore core,
    HZPMenuHelper menuHelper,
    HZPHelpers helpers,
    HZPStoreService storeService)
{
    public IMenuAPI OpenAdminItemMenu(IPlayer player)
    {
        IMenuAPI menu = menuHelper.CreateMenu(helpers.T(player, "AdminItemMenu"));

        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            helpers.T(player, "AdminMenuSelect"),
            Color.Red, Color.LightBlue, Color.Red),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        var items = storeService.GetAdminItems().ToList();
        foreach (var item in items)
        {
            var button = new ButtonMenuOption(item.DisplayName)
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

                    var result = storeService.TryGrantAdminItem(clicker, item);
                    if (!result.Success)
                    {
                        clicker.SendMessage(MessageType.Chat, helpers.T(clicker, result.MessageKey));
                        return;
                    }

                    clicker.SendMessage(MessageType.Chat, helpers.T(clicker, "AdminItemGranted", item.DisplayName));
                });
            };

            menu.AddOption(button);
        }

        core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }
}
