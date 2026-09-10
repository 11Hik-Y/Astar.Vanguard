
using System.Runtime.CompilerServices;
using CommonAssets.Scripts.Game.LabyrinthEvent;
using Astar.Vanguard.Client.Datas;

namespace Astar.Vanguard.Client.Extensions
{
    public static class TrapSyncableExtensions
    {
        private static readonly ConditionalWeakTable<TrapSyncable, BarbedWireData> _dataDict = new();
        
        extension(TrapSyncable trapSyncable)
        {
            public BarbedWireData GetData()
            {
                return _dataDict.TryGetValue(trapSyncable, out BarbedWireData data) ? data : trapSyncable.InitData();
            }

            public BarbedWireData InitData()
            {
                var data = new BarbedWireData(trapSyncable);
                _dataDict.Add(trapSyncable, data);
                return data;
            }
        }
    }
}