
using SPTarkov.Server.Core.Models.Common;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Astar.Vanguard.Server.Models.Eft.Common.Tables
{
    public record OrderInfo : BaseInfo
    {
        [JsonPropertyName("PlayerIds")]
        public required HashSet<MongoId> PlayerIds { get; set; }

        [JsonPropertyName("SpawnType")]
        public required SpawnType SpawnType { get; set; }

        [JsonPropertyName("CarryServiceLevel")]
        public required int CarryServiceLevel { get; set; }

        [JsonPropertyName("Duration")]
        public required int Duration { get; set; }

        [JsonPropertyName("RenewTargetQuestId")]  
        public MongoId? RenewTargetQuestId { get; set; }
    }
}