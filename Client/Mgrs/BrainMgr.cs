
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using Astar.Vanguard.Client.Bots.Brain.Layers;
using Astar.Vanguard.Client.Events;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Misc;
using Astar.Vanguard.Client.Models;
using Astar.Vanguard.Client.Utils;

namespace Astar.Vanguard.Client.Mgrs
{
    internal class BrainMgr : BaseMgr
    {
        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        
        public override void Start()
        {
            base.Start();
            InitCustomLayerMaps();
            Gameloop.IsGameStarted = Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance is not HideoutGameWorld;
            Gameloop.CheckVaildGameWorld();
            if (AstarVanguardPlugin.IsLoadedByScriptEngine && Gameloop.IsVaildGameWorld)
            {
                McsMgr.IsHost = true;
                EventMgr.Notify(new GameWorldStartedEvent
                {
                    GameWorld = Singleton<GameWorld>.Instance,
                });
                TasksExtensions.HandleExceptions(Reload());
            }
        }

        private void InitCustomLayerMaps()
        {
            LayerUtils.RegisterCustomLayer(typeof(McsCommonLayer), 65);
            LayerUtils.RegisterCustomLayer(typeof(McsAvoidDangerLayer), 66);
            LayerUtils.RegisterCustomLayer(typeof(McsClearAreaLayer), 67);
            LayerUtils.RegisterCustomLayer(typeof(McsExfiltrationLayer), 68);
            LayerUtils.RegisterCustomLayer(typeof(McsFightLayer), 185);
            LayerUtils.RegisterCustomLayer(typeof(McsEscortLayer), 186);
            LayerUtils.RegisterCustomLayer(typeof(McsProxyLayer), 187);
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            LayerUtils.OnRaidEnded();
        }

        private async Task Reload()
        {
            var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (myPlayer == null)
            {
                return;
            }

            var mcsProfilesDict = await McsRequestHandler.GetMcsBotPlayers(new()
            {
                Side = myPlayer.Side is EPlayerSide.Bear or EPlayerSide.Usec ? ESideType.Pmc : ESideType.Savage
            });

            if (mcsProfilesDict.Count == 0)
            {
                return;
            }

            await Gameloop.InitMcsLeadPlayerConfigs();

            var leadPlayers = mcsProfilesDict
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => Singleton<GameWorld>.Instance.GetEverExistedPlayerByID(kvp.Key))
                .Where(leadPlayer => leadPlayer != null);

            foreach (var leadPlayer in leadPlayers)
            {
                if (leadPlayer.ProfileId == myPlayer.ProfileId)
                {
                    var mcsBotPlayerConfig = new McsBotPlayerConfig
                    {
                        McsLeadPlayerId = leadPlayer.ProfileId,
                        EnableLooting = AstarVanguardPlugin.EnableLooting.Value,
                        PriceThreshold = AstarVanguardPlugin.PriceThreshold.Value,
                        KeywordItemText = AstarVanguardPlugin.KeywordItemText.Value,
                        LootingKeywordItem = AstarVanguardPlugin.LootingKeywordItem.Value,
                        BlockItemType = (int)AstarVanguardPlugin.BlockItemType.Value,
                        EnableKeepFormation = AstarVanguardPlugin.EnableKeepFormation.Value,
                        FormationMatrix = AstarVanguardPlugin.FormationMatrix.Value,
                        FormationSpacing = AstarVanguardPlugin.FormationSpacing.Value,
                        FormationSequentialFill = AstarVanguardPlugin.FormationSequentialFill.Value,
                    };
                    McsMgr.UpdateMcsBotPlayerConfig(mcsBotPlayerConfig.McsLeadPlayerId, mcsBotPlayerConfig);
                }
                var mcsAILeadPlayer = new McsAILeadPlayer(leadPlayer);
                foreach (var mcsProfileItem in mcsProfilesDict)
                {
                    foreach (var mcsBotPlayerProfile in mcsProfileItem.Value)
                    {
                        var mcsBotPlayer = McsMgr.TryGetMcsBotPlayer(mcsBotPlayerProfile.ProfileId, leadPlayer.ProfileId);
                        var baseBrain = mcsBotPlayer?.BotOwner?.Brain?.BaseBrain;
                        if (baseBrain == null)
                        {
                            continue;
                        }
                        McsMgr.AddMcsSquadMember(leadPlayer.ProfileId, mcsBotPlayer.ProfileId, mcsAILeadPlayer);
                        var botOwner = mcsBotPlayer.BotOwner;
                        var wildSpawnType = mcsBotPlayer.Profile.Info.Settings.Role;
                        var botDifficulty = mcsBotPlayer.Profile.Info.Settings.BotDifficulty;
                        var settings = Gameloop.SetBotSettings(botDifficulty, wildSpawnType, botOwner, leadPlayer);
                        botOwner.Settings = settings;
                        InjectLayers(baseBrain);
                    }
                }
            }
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();

            var mcsBotPlayers = McsMgr.GetAllMcsBotPlayer();
            foreach (var mcsBotPlayer in mcsBotPlayers)
            {
                if (mcsBotPlayer == null)
                {
                    continue;
                }

                LayerUtils.McsRestoreLayers(mcsBotPlayer.AIData.BotOwner, Classification.RemoveLayerNames);

                var customLayerMaps = LayerUtils.GetCustomLayerMaps();
                foreach ((var customLayerType, var priority) in customLayerMaps)
                {
                    LayerUtils.McsRemoveLayer(mcsBotPlayer.AIData.BotOwner, customLayerType.Name);
                }
            }
        }

        public void InjectLayers(BaseBrain baseBrain)
        {
            LayerUtils.McsRemoveLayers(baseBrain.Owner, Classification.RemoveLayerNames);

            var customLayerMaps = LayerUtils.GetCustomLayerMaps();
            foreach ((var customLayerType, var priority) in customLayerMaps)
            {
                LayerUtils.McsAddCustomLayer(baseBrain.Owner, customLayerType, priority);
            }
        }
    }
}