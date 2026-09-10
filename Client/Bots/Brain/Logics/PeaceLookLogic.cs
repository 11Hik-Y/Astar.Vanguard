
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace Astar.Vanguard.Client.Bots.Brain.Logics
{
    public class PeaceLookLogic : McsBotBaseLogic
    {
        private PeaceLookBaseLogic _baseLogic;

        public PeaceLookLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByMain(data);
        }
    }
}