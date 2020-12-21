﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotAdminHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly ICovidRepository covidRepository;
        private readonly BotPollResultSenderService botPollResultSender;
        private readonly IOptions<BotSettings> botOptions;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollRepository pollRepository;
        private readonly IChatRepository chatRepository;

        public BotAdminHandler(BotClientService botClientService,
                               IBotCommandHelper botCommandHelper,
                               ICovidRepository covidRepository,
                               BotPollResultSenderService botPollResultSender,
                               IOptions<BotSettings> botOptions,
                               IPollChatRankingRepository pollChatRankingRepository,
                               IPollRepository pollRepository,
                               IChatRepository chatRepository)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.covidRepository = covidRepository;
            this.botPollResultSender = botPollResultSender;
            this.botOptions = botOptions;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollRepository = pollRepository;
            this.chatRepository = chatRepository;
        }

        public IList<BotCommand> Command => null;

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnMessage += BotClient_OnMessage;
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            var command = await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.setCovid, e.Message.Text);
            if (command.CommandCorrect)
            {
                if (e.Message.From.Id == botOptions.Value.AdminUserId)
                {
                    if (int.TryParse(command.CommandArg, out var newCases))
                    {
                        var latestCovid = await covidRepository.FindLatestAsync();
                        if (latestCovid != null && DateTime.UtcNow.Date <= latestCovid.Date)
                            return;

                        await covidRepository.AddAsync(new Db.Covid
                        {
                            NewCases = newCases,
                            Date = DateTime.UtcNow.Date
                        });
                        botPollResultSender.SendPredictionsResultsToChats();
                    }
                }
            }
        }
    }
}
