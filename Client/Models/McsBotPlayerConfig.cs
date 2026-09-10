
using System.Collections.Generic;
using System.Runtime.Serialization;
using EFT;

namespace Astar.Vanguard.Client.Models
{
    [DataContract]
    public class McsBotPlayerConfig
    {
        [DataMember(Name = "McsLeadPlayerId")]
        public MongoID McsLeadPlayerId;

        [DataMember(Name = "EnableLooting")]
        public bool EnableLooting = AstarVanguardPlugin.EnableLooting.Value;

        [DataMember(Name = "PriceThreshold")]
        public int PriceThreshold = AstarVanguardPlugin.PriceThreshold.Value;

        [DataMember(Name = "KeywordItemText")]
        public string KeywordItemText = AstarVanguardPlugin.KeywordItemText.Value;

        [DataMember(Name = "LootingKeywordItem")]
        public bool LootingKeywordItem = AstarVanguardPlugin.LootingKeywordItem.Value;

        [DataMember(Name = "BlockItemType")]
        public int BlockItemType = (int)AstarVanguardPlugin.BlockItemType.Value;

        [DataMember(Name = "FormationMatrix")]
        public string FormationMatrix = AstarVanguardPlugin.FormationMatrix.Value;

        [DataMember(Name = "EnableKeepFormation")]
        public bool EnableKeepFormation = AstarVanguardPlugin.EnableKeepFormation.Value;

        [DataMember(Name = "FormationSpacing")]
        public float FormationSpacing = AstarVanguardPlugin.FormationSpacing.Value;

        [DataMember(Name = "FormationSequentialFill")]
        public bool FormationSequentialFill = AstarVanguardPlugin.FormationSequentialFill.Value;

        [DataMember(Name = "Extensions")]
        public Dictionary<string, McsValue> Extensions = new();
    }
}