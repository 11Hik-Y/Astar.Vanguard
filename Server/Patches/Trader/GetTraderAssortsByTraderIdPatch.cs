
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace Astar.Vanguard.Server.Patches.Trader
{
    /// <summary>
    /// 处于护航库存模式时，使星锋商人能够正常交易
    /// </summary>
    public sealed class GetTraderAssortsByTraderIdPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TraderHelper), nameof(TraderHelper.GetTraderAssortsByTraderId));

        private static Controllers.TraderController TraderController { get => field ??= ServiceLocator.ServiceProvider.GetService<Controllers.TraderController>(); }

        [PatchPrefix]  
        public static bool Prefix(MongoId traderId, ref TraderAssort? __result)  
        {  
            if (traderId != Services.TraderService.VanguardTraderId)
            {
                return true;
            }

            __result = TraderController.GetMcsBotPlayerInventoryModeAssort();
            return false;
        }
    }
}