using System.Reflection;
using EFT;
using HarmonyLib;
using Astar.Vanguard.Client.Utils;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.Events
{
    /// <summary>
    /// 借鉴fika。实现护航的击杀经验共享给老板
    /// </summary>
    public sealed class OnBeenKilledByAggressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Player), nameof(Player.OnBeenKilledByAggressor));

        [PatchPostfix]
        public static void Postfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            if (!AstarVanguardPlugin.FikaInstalled || AstarVanguardPlugin.IsLoadedByScriptEngine)
            {
                Tools.HandleSharedExperience(__instance, aggressor);
                Tools.HandleSharedQuestCondition(__instance, aggressor, damageInfo, bodyPart);
            }
        }
    }
}