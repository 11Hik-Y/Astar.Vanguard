using System.Reflection;
using Astar.Vanguard.Client.UI;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Astar.Vanguard.Client.Patches.RefreshQuests
{
    /// <summary>
    /// EFT 40087: TraderScreensGroup.method_6(TraderClass) is the actual
    /// trader-card selection path. The fixed Vanguard trader opens the
    /// Astar.UI command center instead of relying on the vanilla assort screen.
    /// </summary>
    public sealed class VanguardTraderSelectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(TraderScreensGroup),
                "method_6",
                new[] { typeof(TraderClass) }
            );
        }

        [PatchPostfix]
        public static void Postfix(
            TraderScreensGroup __instance,
            TraderClass nextSelected
        )
        {
            if (
                nextSelected is null
                || nextSelected.Id != AstarVanguardPlugin.VanguardTraderId
            )
            {
                return;
            }

            VanguardCommandCenterScreen.TryOpen(__instance);
        }
    }
}
