
using System.Threading.Tasks;
using Astar.Vanguard.Server.Controllers;
using Astar.Vanguard.Server.Models.Eft.Trader;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace Astar.Vanguard.Server.Callbacks
{
    [Injectable]
    public class TraderCallbacks(
        HttpResponseUtil httpResponseUtil,
        TraderController traderController
    )
    {
        /// <summary>
        /// 处理 /mcs/client/trading/api/friendlyFirePenalty
        /// </summary>
        public virtual async ValueTask<string> FriendlyFirePenalty(string url, FriendlyFirePenaltyRequestData info, MongoId mcsLeadPlayerId)
        {
            traderController.FriendlyFirePenalty(mcsLeadPlayerId, info);
            return httpResponseUtil.NullResponse();
        }

        /// <summary>
        /// 处理 /mcs/client/trading/api/compensation
        /// </summary>
        public virtual async ValueTask<string> Compensation(string url, CompensationRequestData info, MongoId mcsLeadPlayerId)
        {
            traderController.Compensation(info);
            return httpResponseUtil.NullResponse();
        }

        /// <summary>
        /// 处理 /mcs/client/trading/api/updateProfile
        /// </summary>
        public virtual async ValueTask<string> UpdateProfile(string url, EmptyRequestData info, MongoId mcsLeadPlayerId)
        {
            return httpResponseUtil.NoBody(await traderController.UpdateProfile(mcsLeadPlayerId));
        }
    }
}