using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace Astar.Vanguard.Client.Bots.Brain.Logics
{
    public class RunAwayGrenadeLogic : McsBotBaseLogic
    {
        private RunAwayGrenadeBaseLogic _baseLogic;

        public RunAwayGrenadeLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}