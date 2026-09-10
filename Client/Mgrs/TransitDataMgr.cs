
using EFT.Interactive;
using Astar.Vanguard.Client.Extensions;

namespace Astar.Vanguard.Client.Mgrs
{
    public class TransitDataMgr : GameWorldDataMgr
    {
        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            LoadData(LoadTransitPoints);
        }

        private void LoadTransitPoints()
        {
            foreach (var transitPoint in LocationScene.GetAllObjects<TransitPoint>())
            {
                var data = transitPoint.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }
    }
}