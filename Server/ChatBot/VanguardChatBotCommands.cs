using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astar.Vanguard.Server.Interface;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;

namespace Astar.Vanguard.Server.ChatBot
{
    [Injectable]
    public class VanguardChatBotCommands(IEnumerable<IMcsCommand> vanguardCommands) : IMcsChatCommand
    {
        protected readonly IDictionary<string, IMcsCommand> _vanguardCommands = vanguardCommands.ToDictionary(c => c.Command);

        public string[] GetCommandHelps(string command)
        {
            return _vanguardCommands.TryGetValue(command, out IMcsCommand value) ? value.CommandHelps : [];
        }

        public string CommandPrefix
        {
            get
            {
                return "mcs";
            }
        }

        public List<string> Commands
        {
            get
            {
                return [.. _vanguardCommands.Keys];
            }
        }

        public async ValueTask<string> Handle(string command, UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
        {
            return await _vanguardCommands[command].PerformAction(commandHandler, sessionId, request);
        }
    }
}