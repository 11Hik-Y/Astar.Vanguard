
using System.Runtime.CompilerServices;
using EFT.SynchronizableObjects;
using Astar.Vanguard.Client.Datas;

namespace Astar.Vanguard.Client.Extensions
{
    public static class TripwireSynchronizableObjectExtensions
    {
        private static readonly ConditionalWeakTable<TripwireSynchronizableObject, TripwireData> _dataDict = new();
        
        extension(TripwireSynchronizableObject tripwireSynchronizableObject)
        {
            public TripwireData GetData()
            {
                return _dataDict.TryGetValue(tripwireSynchronizableObject, out TripwireData data) ? data : tripwireSynchronizableObject.InitData();
            }

            public TripwireData InitData()
            {
                var data = new TripwireData(tripwireSynchronizableObject);
                _dataDict.Add(tripwireSynchronizableObject, data);
                return data;
            }
        }
    }
}