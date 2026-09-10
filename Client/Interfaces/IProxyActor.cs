namespace Astar.Vanguard.Client.Interfaces
{
    public interface IProxyActor : IActor
    {
        public abstract string Id();
        public abstract bool IsProxyActionDisabled();
    }
}