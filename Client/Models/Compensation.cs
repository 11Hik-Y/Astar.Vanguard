
using System.Runtime.Serialization;
using EFT;

namespace Astar.Vanguard.Client.Models
{
    [DataContract]
    public class Compensation
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId;
    }
}