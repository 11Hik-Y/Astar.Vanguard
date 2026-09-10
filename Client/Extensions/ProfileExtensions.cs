
using EFT;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Utils;

namespace Astar.Vanguard.Client.Extensions
{
    public static class ProfileExtensions
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();
        
        extension(Profile profile)
        {
            public string McsNickname => AstarVanguardPlugin.ShowBrevityCode.Value ? "Rabbit" + McsMgr.GetMcsBotPlayerIndex(profile.Id, false) : profile.Nickname;
        }
    }
}