
using System.Reflection;
using EFT;
using HarmonyLib;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Bots
{
    /// <summary>
    /// 只有主机才会执行此函数，用于标识自身为主机
    /// </summary>
    public sealed class BotsControllerInitPatch : ModulePatch
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BotsController), nameof(BotsController.Init));

        [PatchPostfix]
        public static void Postfix()
        {
            McsMgr.IsHost = true;
            TasksExtensions.HandleExceptions(GameLoop.Instance.SpawnMcsBotPlayer());
        }
    }
}