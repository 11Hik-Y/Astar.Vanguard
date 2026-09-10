
using EFT;
using Astar.Vanguard.Client.Interfaces;

namespace Astar.Vanguard.Client.Events
{
    public class McsBotPlayerActivatedEvent : IMcsEvent
    {
        public MongoID McsBotPlayerId { get; set; }
    }
}