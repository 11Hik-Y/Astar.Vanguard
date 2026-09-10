using System.Collections.Generic;

namespace Astar.Vanguard.Client.Models
{
    public sealed class VanguardOperatorState
    {
        public string Id { get; set; }
        public string CodeName { get; set; }
        public string Role { get; set; }
        public float Aim { get; set; }
        public float Vision { get; set; }
        public float Hearing { get; set; }
        public float Reaction { get; set; }
        public float Aggression { get; set; }
        public float DamageCoeff { get; set; }
        public float EnemyMemory { get; set; }
        public float Cover { get; set; }
        public bool Recruited { get; set; }
        public bool Deployed { get; set; }
        public string ProfileId { get; set; }
        public int? Aid { get; set; }
    }

    public sealed class VanguardCommandCenterSnapshot
    {
        public List<VanguardOperatorState> Operators { get; set; } = new();
        public List<string> Deployment { get; set; } = new();
        public int MaxDeployment { get; set; } = 4;
    }

    public sealed class VanguardRecruitRequest
    {
        public string OperatorId { get; set; }
    }

    public sealed class VanguardDeploymentRequest
    {
        public List<string> OperatorIds { get; set; } = new();
    }
}
