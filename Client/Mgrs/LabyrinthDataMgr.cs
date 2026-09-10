

using Comfort.Common;
using EFT;

namespace Astar.Vanguard.Client.Mgrs
{
    public abstract class LabyrinthDataMgr : DataMgr
    {
        protected bool _shouldInit => Singleton<GameWorld>.Instance.LocationId == "Labyrinth";
    }
}