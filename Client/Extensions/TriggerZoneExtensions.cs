
using System.Runtime.CompilerServices;
using EFT.GameTriggers;
using Astar.Vanguard.Client.Datas;

namespace Astar.Vanguard.Client.Extensions
{
    public static class TriggerZoneExtensions
    {
        private static readonly ConditionalWeakTable<TriggerZone, TriggerZoneData> _dataDict = new();
        
        extension(TriggerZone triggerZone)
        {
            public TriggerZoneData GetData(string triggerZoneId)
            {
                return _dataDict.TryGetValue(triggerZone, out TriggerZoneData data) ? data : triggerZone.InitData(triggerZoneId);
            }

            public TriggerZoneData InitData(string triggerZoneId)
            {
                var data = new TriggerZoneData(triggerZone, triggerZoneId);
                _dataDict.Add(triggerZone, data);
                return data;
            }
        }
    }
}