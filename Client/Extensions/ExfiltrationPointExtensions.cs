
using System.Runtime.CompilerServices;
using EFT.Interactive;
using Astar.Vanguard.Client.Datas;

namespace Astar.Vanguard.Client.Extensions
{
    public static class ExfiltrationPointExtensions
    {
        private static readonly ConditionalWeakTable<ExfiltrationPoint, ExfilData> _dataDict = new();
        
        extension(ExfiltrationPoint exfiltrationPoint)
        {
            public ExfilData GetData()
            {
                return _dataDict.TryGetValue(exfiltrationPoint, out ExfilData data) ? data : exfiltrationPoint.InitData();
            }

            public ExfilData InitData()
            {
                var data = new ExfilData(exfiltrationPoint);
                _dataDict.Add(exfiltrationPoint, data);
                return data;
            }
        }
    }
}