
using System.Runtime.Serialization;

namespace Astar.Vanguard.Client.Models
{
    [DataContract]
    public class DebugInfo
    {
        [DataMember(Name = "Info")]
        public string Info;
    }
}