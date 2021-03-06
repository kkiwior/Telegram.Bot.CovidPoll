﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotRankingHandler : IBotEvent
    {
        private readonly IBotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IUserRatioRepository userRatioRepository;

        public BotRankingHandler(IBotClientService botClientService, IBotCommandHelper botCommandHelper, 
            IPollChatRankingRepository pollChatRankingRepository, IUserRatioRepository userRatioRepository)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.userRatioRepository = userRatioRepository;
        }

        public IList<BotCommand> Command => 
            new List<BotCommand> 
            { 
                new BotCommand 
                { 
                    Command = BotCommands.ranking.ToString(), 
                    Description = "Wyświetla ranking osób najlepiej przewidujących."
                } 
            };

        public void RegisterEvent(IBotClientService botClient)
        {
            botClient.BotClient.OnMessage += BotClient_OnMessage;
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            var n = await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.ranking, e.Message.Text);
            if (n.CommandCorrect && 
                (e.Message.Chat.Type == ChatType.Supergroup || e.Message.Chat.Type == ChatType.Group))
            {
                var ranking = await pollChatRankingRepository.GetChatRankingAsync(e.Message.Chat.Id);
                if (ranking == null || ranking.LastCommandDate.AddSeconds(4) >= DateTime.UtcNow)
                    return;

                await pollChatRankingRepository
                    .UpdateLastCommandDateAsync(e.Message.Chat.Id, DateTime.UtcNow);

                var sb = new StringBuilder("<b>Ogólny ranking:</b>\n");
                if (ranking.Winners.Count == 0)
                    sb.AppendLine("\nBrak osób, które poprawnie przewidziały.");

                var usersRatio = await userRatioRepository.GetAsync(e.Message.Chat.Id);
                foreach (var winner in ranking.Winners
                    .OrderByDescending(w => w.Points).Select((value, index) => new { value, index }))
                {
                    var userRatio = usersRatio
                        .FirstOrDefault(ur => ur.UserId == winner.value.UserId)?.Ratio;

                    if (userRatio != null)
                    {
                        sb.AppendLine(
                            $"{winner.index + 1}. {winner.value.Username ?? winner.value.UserFirstName}" +
                            $" - {winner.value.Points} punkty/ów ({userRatio:N3})");
                    }
                    else
                    {
                        sb.AppendLine(
                            $"{winner.index + 1}. {winner.value.Username ?? winner.value.UserFirstName}" +
                            $" - {winner.value.Points} punkty/ów");
                    }
                }

                sb.AppendLine("\nKomendę można wywołać co 4 sekundy.");
                try
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: sb.ToString(),
                        parseMode: ParseMode.Html
                    );
                }
                catch (Exception) {}
            }
        }
    }
}
