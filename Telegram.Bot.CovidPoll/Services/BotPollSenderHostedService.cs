using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollSenderHostedService : BackgroundService
    {
        private readonly IBotClientService botClientService;
        private readonly IOptions<BotSettings> botSettings;
        private readonly IOptions<CovidTrackingSettings> covidSettings;
        private readonly IChatRepository chatRepository;
        private readonly IPollRepository pollRepository;
        private readonly IPollOptionsService pollOptionsService;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IBotPollResultSenderService botPollResultSender;
        private readonly IPollConverterHelper pollConverterHelper;
        private readonly IQueueService queueService;
        private readonly IBotMessageHelper botMessageHelper;

        public BotPollSenderHostedService(IBotClientService botClientService,
                                          IOptions<BotSettings> botSettings,
                                          IOptions<CovidTrackingSettings> covidSettings,
                                          IChatRepository chatRepository,
                                          IPollRepository pollRepository,
                                          IPollOptionsService pollOptionsService,
                                          IPollChatRepository pollChatRepository,
                                          IBotPollResultSenderService botPollResultSender,
                                          IPollConverterHelper pollConverterHelper,
                                          IQueueService queueService,
                                          IBotMessageHelper botMessageHelper)
        {
            this.botClientService = botClientService;
            this.botSettings = botSettings;
            this.covidSettings = covidSettings;
            this.chatRepository = chatRepository;
            this.pollRepository = pollRepository;
            this.pollOptionsService = pollOptionsService;
            this.pollChatRepository = pollChatRepository;
            this.botPollResultSender = botPollResultSender;
            this.pollConverterHelper = pollConverterHelper;
            this.queueService = queueService;
            this.botMessageHelper = botMessageHelper;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var pollsStart = DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsStartHourUtc);
            var pollsEnd = DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsEndHourUtc);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= pollsStart && DateTime.UtcNow <= pollsEnd)
                {
                    if (await SendPolls(stoppingToken))
                    {
                        pollsStart = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsStartHourUtc);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                }
                else if (DateTime.UtcNow >= pollsEnd)
                {
                    await StopPolls(stoppingToken);
                    pollsEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsEndHourUtc);
                    botPollResultSender.SendPredictionsToChats();
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<bool> SendPolls(CancellationToken stoppingToken)
        {
            //Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting sending polls...");
            var poll = await pollOptionsService.GetPollOptionsAsync(DateTime.UtcNow.Date);
            var chats = await chatRepository.GetAll();
            if (poll == null)
            {
                /*queueService.QueueBackgroundWorkItem(async stoppingToken =>
                {
                    foreach (var chat in chats)
                    {
                        try
                        {
                            await botClientService.BotClient.SendTextMessageAsync(
                                chatId: chat.ChatId,
                                text:
                                "Niestety, nie posiadamy aktualnych wyników zakażeń, aby wyświetlić wyniki przewidywań i udostępnić nową ankietę. Kolejna próba nastąpi za 1h.",
                                cancellationToken: stoppingToken
                            );
                        }
                        catch (Exception) { }
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                });*/
                //Log.Information(
                    //$"[{nameof(BotPollSenderHostedService)}] - Polls haven't been sent. Not enough information about covid.");
            }
            else if (!poll.ChatPollsSended)
            {
                await pollRepository.SetSendedAsync(poll.Id, true);
                var convertedPollOptions = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options);

                queueService.QueueBackgroundWorkItem(async stoppingToken =>
                {
                    foreach (var chat in chats)
                    {
                        try
                        {
                            var sendedPoll = await SendPoll(chat.ChatId, convertedPollOptions, stoppingToken);
                            await pollChatRepository.AddAsync(poll.Id, new Db.PollChat()
                            {
                                ChatId = sendedPoll.PollMessage.Chat.Id,
                                PollId = sendedPoll.PollMessage.Poll.Id,
                                MessageId = sendedPoll.PollMessage.MessageId,
                                NonPollMessageId = sendedPoll.NonPollMessage.MessageId
                            });
                        }
                        catch (Exception) { }

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                });
                //Log.Information($"[{nameof(BotPollSenderHostedService)}] - Polls have been sent.");

                return true;
            }
            else if (poll.ChatPollsSended)
            {
                return true;
            }
            return false;
        }

        private async Task StopPolls(CancellationToken stoppingToken)
        {
            //Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting closing polls...");
            var poll = await pollRepository.FindLatestAsync();
            if (poll == null || poll.ChatPollsClosed)
            {
                //Log.Information($"[{nameof(BotPollSenderHostedService)}] - There is nothing to close.");
                return;
            }

            var chatPolls = poll.ChatPolls;
            if (chatPolls != null)
            {
                queueService.QueueBackgroundWorkItem(async stoppingToken =>
                {
                    await pollRepository.SetClosedAsync(poll.Id, true);
                    foreach (var chatPoll in chatPolls)
                    {
                        try
                        {
                            await StopPoll(chatPoll.ChatId, chatPoll.MessageId, stoppingToken);
                        }
                        catch (Exception) { }

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                });
            }
            //Log.Information($"[{nameof(BotPollSenderHostedService)}] - All polls have been closed.");
        }

        private Task StopPoll(long chatId, int messageId, CancellationToken stoppingToken)
        {
            return botClientService.BotClient.StopPollAsync(chatId, messageId, cancellationToken: stoppingToken);
        }

        private Task<SendPollModel> SendPoll(long chatId, IList<string> pollOptions, CancellationToken stoppingToken)
        {
            return botMessageHelper.SendPollAsync(chatId, pollOptions, stoppingToken);
        }
    }
}