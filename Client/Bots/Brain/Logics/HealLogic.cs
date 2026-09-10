
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Models;

namespace Astar.Vanguard.Client.Bots.Brain.Logics
{
    public class HealLogic : McsBotBaseLogic
    {
        private HealOverrideLogic _baseLogic;

        public HealLogic(BotOwner botOwner) : base(botOwner)
        {
            _baseLogic = new(botOwner);
        }

        public override void Start()
        {
            base.Start();
            BotOwner.TalkMsg(new McsMsg
            {
                PhraseTrigger = EPhraseTrigger.StartHeal,
            });
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(CustomLayer.ActionData data)
        {
            _baseLogic.UpdateNodeByBrain(data);
        }
    }
}