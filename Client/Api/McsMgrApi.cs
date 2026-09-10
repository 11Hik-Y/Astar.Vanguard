
using Astar.Vanguard.Client.Interfaces;
using Astar.Vanguard.Client.Utils;

namespace Astar.Vanguard.Client.Api
{
    public static class McsMgrApi
    {
        /// <summary>
        /// 
        /// </summary>
        public static T GetMgr<T>() where T : IMgr
        {
            return MgrAccessor.Get<T>();
        }
    }
}