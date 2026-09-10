
using Astar.Vanguard.Client.Interfaces;
using UnityEngine;

namespace Astar.Vanguard.Client.Datas
{
    public abstract class WorldData : BaseData, IActor
    {
        public abstract string GetActionName();
        public abstract string GetActionTargetName(Vector3 myPlayerPos);
        public abstract bool IsDisabled();
        public abstract Vector3 GetPos();
    }
}