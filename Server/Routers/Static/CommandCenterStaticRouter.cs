using Astar.Vanguard.Server.Callbacks;
using Astar.Vanguard.Server.Models.Mcs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace Astar.Vanguard.Server.Routers.Static;

[Injectable]
public sealed class CommandCenterStaticRouter(
    JsonUtil jsonUtil,
    CommandCenterCallbacks callbacks
) : StaticRouter(
    jsonUtil,
    [
        new RouteAction<EmptyRequestData>(
            "/astar/vanguard/command-center",
            async (url, info, sessionId, output) =>
                await callbacks.GetSnapshot(url, info, sessionId)
        ),
        new RouteAction<VanguardRecruitRequest>(
            "/astar/vanguard/recruit",
            async (url, info, sessionId, output) =>
                await callbacks.Recruit(url, info, sessionId)
        ),
        new RouteAction<VanguardDeploymentRequest>(
            "/astar/vanguard/deployment",
            async (url, info, sessionId, output) =>
                await callbacks.SetDeployment(url, info, sessionId)
        )
    ]
)
{ }
