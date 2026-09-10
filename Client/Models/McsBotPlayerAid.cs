
using System.Runtime.Serialization;

namespace Astar.Vanguard.Client.Models
{
    [DataContract]
    public class McsBotPlayerAid
    {
        [DataMember(Name = "Aid")]
        public string Aid;
    }
}