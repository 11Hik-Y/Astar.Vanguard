
using EFT;
using Astar.Vanguard.Client.Interfaces;

namespace Astar.Vanguard.Client.Events
{
    public class OnPlayerDeadEvent : IMcsEvent
    {
        public Player DeadPlayer { get; set; }
    }
}