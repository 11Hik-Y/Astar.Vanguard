using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace Astar.Vanguard.Server.Models.Mcs;

public sealed record VanguardOperatorState
{
    public required string Id { get; init; }
    public required string CodeName { get; init; }
    public required string Role { get; init; }
    public float Aim { get; init; }
    public float Vision { get; init; }
    public float Hearing { get; init; }
    public float Reaction { get; init; }
    public float Aggression { get; init; }
    public float DamageCoeff { get; init; }
    public float EnemyMemory { get; init; }
    public float Cover { get; init; }
    public bool Recruited { get; init; }
    public bool Deployed { get; init; }
    public string? ProfileId { get; init; }
    public int? Aid { get; init; }
}

public sealed record VanguardCommandCenterSnapshot
{
    public required List<VanguardOperatorState> Operators { get; init; }
    public required List<string> Deployment { get; init; }
    public int MaxDeployment { get; init; } = 4;
}

public sealed record VanguardRecruitRequest : IRequestData
{
    [JsonPropertyName("OperatorId")]
    public required string OperatorId { get; init; }
}

public sealed record VanguardDeploymentRequest : IRequestData
{
    [JsonPropertyName("OperatorIds")]
    public required List<string> OperatorIds { get; init; }
}
