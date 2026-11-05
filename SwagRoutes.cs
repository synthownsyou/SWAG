using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SWAG;

[Injectable(TypePriority = OnLoadOrder.Routers)]
public class SwagRoutes(JsonUtil jsonUtil, SwagRouteCallbacks callbacks)
    : StaticRouter(jsonUtil, [
        new RouteAction<EmptyRequestData>(
            "/client/match/local/end",
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleRaidEnd(url, sessionId, output)
        ),
        new RouteAction<EmptyRequestData>(
            "/client/locations",
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleLocations(url, sessionId, output)
        ),
        new RouteAction<EmptyRequestData>(
            "/client/items",
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleItems(url, sessionId, output)
        ),
        new RouteAction<EmptyRequestData>(
            "/client/raid/configuration",
            async (url, info, sessionId, output, cancellationToken) =>
                await callbacks.HandleRaidConfiguration(url, sessionId, output)
        )
    ])
{ }

[Injectable]
public class SwagRouteCallbacks(
    ISptLogger<SwagRouteCallbacks> logger,
    SwagMod swagMod)
{
    public ValueTask<string> HandleRaidEnd(string url, MongoId sessionId, string output)
    {
        swagMod.ClearDefaultSpawns();
        swagMod.ConfigureMaps();
        return new ValueTask<string>(output);
    }

    public ValueTask<string> HandleLocations(string url, MongoId sessionId, string output)
    {
        swagMod.ClearDefaultSpawns();
        swagMod.ConfigureMaps();
        return new ValueTask<string>(output);
    }

    public ValueTask<string> HandleItems(string url, MongoId sessionId, string output)
    {
        swagMod.DisableSpawnSystems();
        return new ValueTask<string>(output);
    }

    public ValueTask<string> HandleRaidConfiguration(string url, MongoId sessionId, string output)
    {
        try
        {
            string tod = "day";
            swagMod.SetBotCaps(tod);
        }
        catch (Exception e)
        {
            logger.Error($"SWAG: Failed to set bot caps. Error: {e}");
        }
        return new ValueTask<string>(output);
    }
}
