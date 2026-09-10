using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Events
{
    public sealed class IsAllowedPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(LighthouseTraderZone), nameof(LighthouseTraderZone.IsAllowedPlayer));

        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        [PatchPostfix]
        public static void Postfix(Player player, List<Player> ___allowedPlayers, ref bool __result)
        {
            if (McsMgr.IsMcsBotPlayer(player.ProfileId))
            {
                var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(player.ProfileId);
                __result = ___allowedPlayers.Contains(mcsLeadPlayer);
            }
        }
    }
}