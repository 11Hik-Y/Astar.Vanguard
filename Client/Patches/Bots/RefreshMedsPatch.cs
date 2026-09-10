
using System.Reflection;
using HarmonyLib;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Bots
{
    /// <summary>
    /// 让护航使用更好的医疗品刷新算法
    /// </summary>
    public sealed class RefreshMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotFirstAidClass), nameof(BotFirstAidClass.RefreshMeds));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(BotFirstAidClass __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.BotOwner_0.ProfileId))
            {
                __instance.McsRefreshMeds();
                return false;
            }
            return true;
        }
    }
}