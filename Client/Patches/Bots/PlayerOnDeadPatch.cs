
using System.Reflection;
using EFT;
using HarmonyLib;
using Astar.Vanguard.Client.Events;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Bots
{
    /// <summary>
    /// 使服务端开启平衡限制时，清除护航的自带物品，并通过广播事件立即清理敌人信息
    /// </summary>
    public sealed class PlayerOnDeadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnDead));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPrefix]
        public static void Prefix(Player __instance, EDamageType damageType)
        {
            if (!Tools.IsHost)
            {
                return;
            }

            if (!AstarVanguardPlugin.McsPluginClientConfig.BalanceRestriction)
            {
                return;
            }

            if (!McsMgr.IsMcsBotPlayer(__instance.ProfileId))
            {
                return;
            }

            var mcsBotPlayerData = __instance.AIData.BotOwner.GetMcsBotPlayerData();
            if (mcsBotPlayerData == null)
            {
                return;
            }

            mcsBotPlayerData.HandleBalanceRestriction();
        }

        [PatchPostfix]
        public static void Postfix(Player __instance, EDamageType damageType)
        {
            if (!Tools.IsHost)
            {
                return;
            }

            EventMgr.Notify(new OnPlayerDeadEvent
            {
                DeadPlayer = __instance
            });
        }
    }
}