
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astar.Vanguard.Server.Services;
using Astar.Vanguard.Server.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace Astar.Vanguard.Server.ChatBot
{
    [Injectable]
    public class VanguardChatBot(
        ISptLogger<VanguardChatBot> logger,
        MailSendService mailSendService,
        ServerLocalisationService serverLocalisationService,
        ProfileService profileService,
        IEnumerable<VanguardChatBotCommands> chatCommands
    ) : IDialogueChatBot
    {
        private static readonly MongoId _vanguardId = new(TraderService.VanguardTraderId);

        protected readonly IDictionary<string, VanguardChatBotCommands> _chatCommands = chatCommands.ToDictionary(command => command.CommandPrefix);

        public UserDialogInfo GetChatBot()
        {
            return new UserDialogInfo
            {
                Id = _vanguardId,
                Aid = 1560107,
                Info = new()
                {
                    Level = 15,
                    MemberCategory = MemberCategory.Developer,
                    SelectedMemberCategory = MemberCategory.Developer,
                    Nickname = "星锋指挥部",
                    Side = "Usec",
                },
            };
        }

        public async ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
        {
            if (request.Text.Length == 0)
            {
                logger.Error(serverLocalisationService.GetText("chatbot-command_was_empty"));

                return request.DialogId;
            }

            if (profileService.IsMcsBotPlayerInventoryMode(sessionId))
            {
                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.VanguardTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.VANGUARDTRADERINVENTORYMODEREFUSE,
                    null
                );

                return string.Empty;
            }

            var splitCommand = request.Text.Split(" ");

            if (
                splitCommand.Length > 1
                && _chatCommands.TryGetValue(splitCommand[0], out var commando)
                && commando.Commands.Contains(splitCommand[1])
            )
            {
                return await commando.Handle(splitCommand[1], GetChatBot(), sessionId, request);
            }

            if (string.Equals(splitCommand.FirstOrDefault(), "help", StringComparison.OrdinalIgnoreCase))
            {
                return await SendPlayerHelpMessage(sessionId, request);
            }

            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.VanguardTraderId,
                MessageType.NpcTraderMessage,
                Locales.VANGUARDTRADERUNRECOGNIZEDCOMMAND,
                null
            );

            return string.Empty;
        }

        protected async ValueTask<string> SendPlayerHelpMessage(MongoId sessionId, SendMessageRequest request)
        {
            mailSendService.SendLocalisedNpcMessageToPlayer(
                sessionId,
                TraderService.VanguardTraderId,
                MessageType.NpcTraderMessage,
                Locales.VANGUARDTRADERAVAILABLECOMMANDSLIST,
                null
            );

            foreach (var chatCommand in _chatCommands.Values)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                mailSendService.SendDirectNpcMessageToPlayer(
                    sessionId,
                    TraderService.VanguardTraderId,
                    MessageType.NpcTraderMessage,
                    string.Format(serverLocalisationService.GetText(Locales.VANGUARDTRADERAVAILABLECOMMANDSPREFIX), chatCommand.CommandPrefix),
                    null
                );

                foreach (var subCommand in chatCommand.Commands)
                {
                    foreach (var commandHelp in chatCommand.GetCommandHelps(subCommand))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        mailSendService.SendDirectNpcMessageToPlayer(
                            sessionId,
                            TraderService.VanguardTraderId,
                            MessageType.NpcTraderMessage,
                            string.Format(serverLocalisationService.GetText(Locales.VANGUARDTRADERSUBCOMMAND), subCommand, commandHelp),
                            null
                        );

                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                mailSendService.SendLocalisedNpcMessageToPlayer(
                    sessionId,
                    TraderService.VanguardTraderId,
                    MessageType.NpcTraderMessage,
                    Locales.VANGUARDTRADERSPECIALHELP,
                    null
                );
            }

            return request.DialogId;
        }
    }
}