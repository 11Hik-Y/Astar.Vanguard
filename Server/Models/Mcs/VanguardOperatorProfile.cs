namespace Astar.Vanguard.Server.Models.Mcs
{
    public sealed record VanguardOperatorProfile
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
    }
}
