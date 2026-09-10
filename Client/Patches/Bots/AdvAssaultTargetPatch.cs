
using System.Reflection;
using HarmonyLib;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Bots
{
    /// <summary>
    /// 阻止此Layer的启用，时常发生在护航击杀敌人后，会使护航像傻逼一样从敌人面前跑开
    /// </summary>
    public sealed class AdvAssaultTargetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Class99), nameof(Class99.ShallUseNow));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(Class99 __instance, ref bool __result)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.BotOwner_0.ProfileId))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}