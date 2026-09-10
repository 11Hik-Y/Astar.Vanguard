
using System.Runtime.Serialization;
using EFT;

namespace Astar.Vanguard.Client.Models
{
    [DataContract]
    public class McsBotPlayerType
    {
        [DataMember(Name = "Side")]
        public ESideType Side;
    }
}