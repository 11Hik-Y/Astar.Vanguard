
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using System.Text.Json.Serialization;

namespace Astar.Vanguard.Server.Models.Eft.Common.Tables
{
    public record McsBotPlayerTypeRequestData : IRequestData
    {
        [JsonPropertyName("Side")]
        public required SideType Side { get; set; }
    }
}