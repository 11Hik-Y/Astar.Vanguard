using EFT;
using Astar.Vanguard.Client.Interfaces;

namespace Astar.Vanguard.Client.Events
{
    public class GameWorldStartedEvent : IMcsEvent
    {
        public GameWorld GameWorld { get; set; }
    }

    public class GameWorldEndedEvent : IMcsEvent
    {
        public ExitStatus ExitStatus { get; set; }
    }
}