
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace Astar.Vanguard.Client.Bots.Brain.Logics
{
    public class AttackMovingLogic : McsBotBaseLogic
    {
        private AttackMovingOverrideLogic _baseLogic;

        public AttackMovingLogic(BotOwner botOwner) : base(botOwner)
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