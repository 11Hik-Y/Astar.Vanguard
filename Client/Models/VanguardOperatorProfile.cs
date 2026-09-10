namespace Astar.Vanguard.Client.Models
{
    public sealed class VanguardOperatorProfile
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
    }
}
