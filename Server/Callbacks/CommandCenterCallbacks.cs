using System.Threading.Tasks;
using Astar.Vanguard.Server.Controllers;
using Astar.Vanguard.Server.Models.Mcs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace Astar.Vanguard.Server.Callbacks;

[Injectable]
public sealed class CommandCenterCallbacks(
    HttpResponseUtil httpResponseUtil,
    CommandCenterController controller
)
{
    public ValueTask<string> GetSnapshot(
        string url,
        EmptyRequestData _,
        MongoId sessionId
    )
    {
        return new(
            httpResponseUtil.NoBody(controller.GetSnapshot(sessionId))
        );
    }

    public ValueTask<string> Recruit(
        string url,
        VanguardRecruitRequest request,
        MongoId sessionId
    )
    {
        return new(
            httpResponseUtil.NoBody(
                controller.Recruit(sessionId, request.OperatorId)
            )
        );
    }

    public async ValueTask<string> SetDeployment(
        string url,
        VanguardDeploymentRequest request,
        MongoId sessionId
    )
    {
        return httpResponseUtil.NoBody(
            await controller.SetDeployment(
                sessionId,
                request.OperatorIds
            )
        );
    }
}
