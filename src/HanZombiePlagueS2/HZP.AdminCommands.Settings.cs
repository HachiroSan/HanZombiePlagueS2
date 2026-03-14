using SwiftlyS2.Shared.Commands;

namespace HanZombiePlagueS2;

public sealed partial class HZPAdminCommands
{
    private void ChangeMapCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, ChangeMapCommandName, "<map_name_or_id>", 1))
            return;

        string mapQuery = string.Join(' ', context.Args).Trim();
        if (string.IsNullOrWhiteSpace(mapQuery))
        {
            ReplySyntax(context, ChangeMapCommandName, "<map_name_or_id>");
            return;
        }

        var map = mapVoteService.FindMap(mapQuery);
        string mapName = map?.Name ?? mapQuery;
        string mapId = string.IsNullOrWhiteSpace(map?.Id) ? mapName : map.Id;

        var state = mapVoteService.State;
        if (state.MapChangeInProgress)
        {
            Reply(context, "AdminCommandChangeMapInProgress");
            return;
        }

        state.VoteActive = false;
        state.VoteCompleted = true;
        state.MapChangeScheduled = true;
        state.ChangeMapImmediately = true;
        state.NextMapName = mapName;
        state.NextMapId = mapId;

        mapVoteService.ChangeMap();
        Reply(context, "AdminCommandChangeMapSender", mapName);
    }

    private void MinPlayersCommand(ICommandContext context)
    {
        if (!HasAdminAccess(context))
            return;

        if (!RequireArgs(context, MinPlayersCommandName, "<count>", 1))
            return;

        if (!TryParseInt(context, context.Args[0], MinPlayersCommandName, "<count>", 1, 64, out int minPlayers))
            return;

        globals.RuntimeMinPlayersToStart = minPlayers;
        Reply(context, "AdminCommandMinPlayersSet", minPlayers);
    }
}
