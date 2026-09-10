
using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Astar.Vanguard.Server.Controllers;
using Astar.Vanguard.Server.Helper;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;

namespace Astar.Vanguard.Server.Patches.Group
{
    /// <summary>
    /// 使玩家进入匹配界面时能够自动加载其他队友的模型
    /// </summary>
    public sealed class GetGroupStatusPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MatchCallbacks), nameof(MatchCallbacks.GetGroupStatus));

        private static RaidController RaidController { get => field ??= ServiceLocator.ServiceProvider.GetService<RaidController>(); }
        private static NotificationHelper NotificationHelper { get => field ??= ServiceLocator.ServiceProvider.GetService<NotificationHelper>(); }
        private static NotificationSendHelper NotificationSendHelper { get => field ??= ServiceLocator.ServiceProvider.GetService<NotificationSendHelper>(); }

        [PatchPostfix]
        public static void Postfix(string url, MatchGroupStatusRequest info, MongoId sessionID)
        {
            // Deployment is the persistent source of truth. Legacy matchmaker
            // group cleanup can clear the transient AI group while moving
            // between screens, so restore it immediately before publishing
            // the ready-model notifications used by the EFT/Fika match UI.
            RaidController.ApplyConfiguredDeployment(sessionID);

            var mcsBotPlayerProfiles = RaidController.GetAllGroupMemberProfiles(sessionID);
            foreach (var mcsBotPlayerProfile in mcsBotPlayerProfiles)
            {
                try
                {
                    // EFT 40087 ignores groupMatchRaidReady for players that are not
                    // already present in GroupPlayers. Permanent Vanguard deployment
                    // bypasses the vanilla invite flow, so first materialize the member
                    // with the normal invite-accept notification, then publish the ready
                    // profile/model update. EFT deduplicates accepted members by AccountId.
                    var acceptNotification =
                        NotificationHelper.GenerateWsGroupMatchInviteAccept(
                            mcsBotPlayerProfile
                        );
                    NotificationSendHelper.SendMessage(
                        sessionID,
                        acceptNotification
                    );

                    var notification = NotificationHelper.GenerateWsGroupMatchRaidReady(mcsBotPlayerProfile, info.IsSavage.Value);
                    NotificationSendHelper.SendMessage(sessionID, notification);
                }
                finally
                {

                }
            }
        }
    }
}