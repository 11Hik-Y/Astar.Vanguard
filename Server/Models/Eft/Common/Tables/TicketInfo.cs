
using System.Text.Json.Serialization;

namespace Astar.Vanguard.Server.Models.Eft.Common.Tables
{
    public record TicketInfo : BaseInfo
    {
        [JsonPropertyName("Percent")]
        public required int Percent { get; set; }
    }
}