using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Threading.Tasks;
using Astar.Vanguard.Server.Services;
using Astar.Vanguard.Server.Patches.Dialogue;
using Astar.Vanguard.Server.Patches.Group;
using Astar.Vanguard.Server.Patches.Friend;
using Astar.Vanguard.Server.Patches.OrderQuest;
using Astar.Vanguard.Server.Patches.Profile;
using Astar.Vanguard.Server.Patches.Trader;

namespace Astar.Vanguard.Server
{
    public class AstarVanguardServer
    {
        [Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
        public class AstarVanguardServerPreLoad(
            ConfigService configService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await configService.OnPreLoadAsync();
                new GetClientRepeatableQuestsPatch().Enable();
                new ChangeRepeatableQuestPatch().Enable();
                new CompleteQuestPatch().Enable();
                new GetOtherProfilePatch().Enable();
                new GetFriendListPatch().Enable();
                new GameStartPatch().Enable();
                new SendGroupInvitePatch().Enable();
                new LeaveGroupPatch().Enable();
                new RemovePlayerFromGroupPatch().Enable();
                new EndLocalRaidPatch().Enable();
                new GetGroupStatusPatch().Enable();
                new SendLocalisedNpcMessageToPlayerPatch().Enable();
                new GenerateDialogueViewPatch().Enable();
                new GetDialogByIdFromProfilePatch().Enable();
                new SptDialogueChatBotPatch().Enable();
                new SaveProfileAsyncPatch().Enable();
                new GetProfilePatch().Enable();
                new ItemEventRouterHandleEventsPatch().Enable();
                new GenerateFleaOffersForTraderPatch().Enable();
                new GetAssortPatch().Enable();
                new GetTraderAssortsByTraderIdPatch().Enable();
                new AddOfferPatch().Enable();
                new RemovePlayerBuildPatch().Enable();
                new SaveEquipmentBuildPatch().Enable();
                new SaveWeaponBuildPatch().Enable();
                new WebSocketDisconnectPatch().Enable();
            }
        }

        [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
        public class AstarVanguardServerPostLoad(
            Services.LocaleService localeService,
            QuestService questService,
            TraderService traderService,
            BuildsService buildsService,
            InfoService infoService,
            ProfileService profileService,
            DeploymentService deploymentService,
            CompatibilityService compatibilityService,
            InventoryService inventoryService
        ) : IOnLoad
        {
            public async Task OnLoad()
            {
                await localeService.OnPostLoadAsync();
                await traderService.OnPostLoadAsync();
                await buildsService.OnPostLoadAsync();
                await infoService.OnPostLoadAsync();
                await profileService.OnPostLoadAsync();
                await deploymentService.OnPostLoadAsync();
                await questService.OnPostLoadAsync();
                await compatibilityService.OnPostLoadAsync();
                await inventoryService.OnPostLoadAsync();
            }
        }
    }
}