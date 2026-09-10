
using System.Reflection;
using HarmonyLib;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Bots
{
    /// <summary>
    /// 此函数也会在最后尝试切换其他武器，遂进行阻止
    /// </summary>
    public sealed class TryReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotReload), nameof(BotReload.TryReload));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static bool Prefix(BotReload __instance)
        {
            if (McsMgr.IsMcsBotPlayer(__instance.BotOwner_0.ProfileId))
            {
                __instance.McsTryReload();
                return false;
            }
            return true;
        }
    }
}