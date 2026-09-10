
using System.Text.Json.Serialization;

namespace Astar.Vanguard.Server.Models.Eft.Common.Tables
{
    public record Punish
    {
        [JsonPropertyName("PunishmentMulti")]
        public required double PunishmentMulti { get; set; }
    }
}