using System.Text.Json.Serialization;
using Astar.Vanguard.Server.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Ws;

namespace Astar.Vanguard.Server.Models.Eft.Ws
{
    public record McsWsGroupMatchRaidReady : WsNotificationEvent
    {
        [JsonPropertyName("extendedProfile")]
        public McsGroupCharacter? ExtendedProfile { get; set; }
    }
}