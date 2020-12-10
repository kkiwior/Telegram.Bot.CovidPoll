﻿using System;
using System.Collections.Generic;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotReplyPollHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollChatRepository pollChatRepository;
        public BotReplyPollHandler(BotClientService botClientService,
                                 IBotCommandHelper botCommandHelper,
                                 IPollChatRankingRepository pollChatRankingRepository,
                                 IPollChatRepository pollChatRepository)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollChatRepository = pollChatRepository;
        }

        public IList<BotCommand> Command =>
            new List<BotCommand>
            {
                new BotCommand
                {
                    Command = "poll",
                    Description = "Wyświetla aktualną ankietę. Jeżeli nie ma żadnej ankiety, to nic nie wyświetli."
                }
            };

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnMessage += BotClient_OnMessage;
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            var n = await botCommandHelper.CheckCommandIsCorrectAsync(BotCommandHelper.BotCommands.poll, e.Message.Text);
            if (n.CommandCorrect && (e.Message.Chat.Type == ChatType.Supergroup || e.Message.Chat.Type == ChatType.Group))
            {
                var pollChat = await pollChatRepository.FindLatestByChatIdAsync(e.Message.Chat.Id);
                if (pollChat == null || pollChat.LastCommandDate.AddSeconds(4) >= DateTime.UtcNow)
                    return;

                await pollChatRepository.UpdateLastCommandDateAsync(e.Message.Chat.Id, DateTime.UtcNow);
                try
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Komendę można wywołać co 4 sekundy.",
                        replyToMessageId: pollChat.MessageId
                    );
                }
                catch (Exception) {}
            }
        }
    }
}