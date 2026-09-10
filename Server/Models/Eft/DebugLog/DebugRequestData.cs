
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace Astar.Vanguard.Server.Models.Eft.Common.Tables
{
    public record DebugRequestData : IRequestData
    {
        [JsonPropertyName("Info")]
        public required string Info { get; set; }
    }
}