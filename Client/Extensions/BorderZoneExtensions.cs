
using System.Runtime.CompilerServices;
using EFT.Interactive;
using Astar.Vanguard.Client.Datas;

namespace Astar.Vanguard.Client.Extensions
{
    public static class BorderZoneExtensions
    {
        private static readonly ConditionalWeakTable<BorderZone, BorderZoneData> _dataDict = new();
        
        extension(BorderZone borderZone)
        {
            public BorderZoneData GetData()
            {
                return _dataDict.TryGetValue(borderZone, out BorderZoneData data) ? data : borderZone.InitData();
            }

            public BorderZoneData InitData()
            {
                var data = new BorderZoneData(borderZone);
                _dataDict.Add(borderZone, data);
                return data;
            }
        }
    }
}