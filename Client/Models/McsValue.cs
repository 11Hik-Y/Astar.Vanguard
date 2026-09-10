
using Astar.Vanguard.Client.Enums;

namespace Astar.Vanguard.Client.Models
{
    public sealed class McsValue
    {
        public EMcsValueType Type;
        public bool BoolValue;
        public long IntValue;
        public float FloatValue;
        public string StringValue;
    }
}