using EFT.Interactive;
using Astar.Vanguard.Client.Interfaces;

namespace Astar.Vanguard.Client.Datas
{
    public abstract class InteractableObjectData : WorldData, IProxyActor
    {
        public abstract string Id();
        public abstract bool IsProxyActionDisabled();
        public abstract InteractableObject GetInteractiveObject();
    }
}