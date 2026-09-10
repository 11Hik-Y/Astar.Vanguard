

using EFT.Interactive;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Utils;

namespace Astar.Vanguard.Client.Mgrs
{
    public class BorderZoneDataMgr : DataMgr
    {
        public override void OnRaidStarted()
        {
            base.OnRaidStarted();
            if (!Tools.IsHost)
            {
                return;
            }
            LoadData(LoadBorderZone);
        }

        private void LoadBorderZone()
        {
            var borderZones = LocationScene.GetAllObjects<BorderZone>();
            foreach (var borderZone in borderZones)
            {
                var data = borderZone.GetData();
                if (data != null)
                {
                    _datas.Add(data);
                }
            }
        }
    }
}