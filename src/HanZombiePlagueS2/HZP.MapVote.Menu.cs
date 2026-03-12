using System.Drawing;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace HanZombiePlagueS2;

public sealed class HZPMapVoteMenu(
    ISwiftlyCore core,
    HZPMenuHelper menuHelper,
    HZPHelpers helpers,
    HZPMapVoteService mapVoteService)
{
    public void OpenVoteMenuForEligiblePlayers(bool forceOpen = false)
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (player == null || !player.IsValid || player.IsFakeClient)
            {
                continue;
            }

            if (!mapVoteService.CanPlayerVote(player))
            {
                continue;
            }

            OpenVoteMenu(player, forceOpen);
        }
    }

    public IMenuAPI? OpenVoteMenu(IPlayer player, bool forceOpen = false)
    {
        if (!mapVoteService.State.VoteActive || player == null || !player.IsValid)
        {
            return null;
        }

        if (!mapVoteService.CanPlayerVote(player))
        {
            return null;
        }

        var currentMenu = core.MenusAPI.GetCurrentMenu(player);
        if (!forceOpen && currentMenu?.Tag?.ToString() == "HZPMapVoteMenu")
        {
            return currentMenu;
        }

        if (currentMenu != null && forceOpen)
        {
            core.MenusAPI.CloseMenuForPlayer(player, currentMenu);
        }

        IMenuAPI menu = menuHelper.CreateMenu(helpers.T(player, "MapVoteTitle"));
        menu.Tag = "HZPMapVoteMenu";
        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            helpers.T(player, "MapVotePrompt", mapVoteService.GetTimeRemaining()),
            Color.LightSkyBlue, Color.LightGreen, Color.LightSkyBlue),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        foreach (var map in mapVoteService.State.MapsInVote)
        {
            int votes = mapVoteService.GetVotes(map.Name);
            string label = $"{map.Name} [{votes}]";
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

                    var result = mapVoteService.TryVote(clicker, map.Name);
                    clicker.SendMessage(MessageType.Chat, helpers.T(clicker, result));
                });
            };

            menu.AddOption(button);
        }

        core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }

    public void CloseAllVoteMenus()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (player == null || !player.IsValid)
            {
                continue;
            }

            var menu = core.MenusAPI.GetCurrentMenu(player);
            if (menu?.Tag?.ToString() == "HZPMapVoteMenu")
            {
                core.MenusAPI.CloseMenuForPlayer(player, menu);
            }
        }
    }
}
