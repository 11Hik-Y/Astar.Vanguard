
using Astar.Vanguard.Server.Callbacks;
using Astar.Vanguard.Server.Models.Eft.Common.Tables;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace Astar.Vanguard.Server.Routers.Static
{
    [Injectable]
    public class LogStaticRouter(
        JsonUtil jsonUtil,
        LogCallbacks logCallbacks
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<DebugRequestData>(
                "/mcs/client/log",
                async (url, info, sessionId, output) => await logCallbacks.PrintLog(url, info, sessionId)
            )
        ]
    )
    { }
}