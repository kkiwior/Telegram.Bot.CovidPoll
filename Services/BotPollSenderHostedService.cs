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
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollSenderHostedService : BackgroundService
    {
        private readonly BotClientService botClientService;
        private readonly IOptions<BotSettings> botSettings;
        private readonly IChatRepository chatRepository;
        private readonly IPollRepository pollRepository;
        private readonly PollOptionsService pollOptionsService;
        private readonly IPollChatRepository pollChatRepository;
        private readonly BotPollResultSenderService botPollResultSender;
        private readonly IPollConverterHelper pollConverterHelper;

        public BotPollSenderHostedService(BotClientService botClientService,
                                          IOptions<BotSettings> botSettings,
                                          IChatRepository chatRepository,
                                          IPollRepository pollRepository,
                                          PollOptionsService pollOptionsService,
                                          IPollChatRepository pollChatRepository,
                                          BotPollResultSenderService botPollResultSender,
                                          IPollConverterHelper pollConverterHelper)
        {
            this.botClientService = botClientService;
            this.botSettings = botSettings;
            this.chatRepository = chatRepository;
            this.pollRepository = pollRepository;
            this.pollOptionsService = pollOptionsService;
            this.pollChatRepository = pollChatRepository;
            this.botPollResultSender = botPollResultSender;
            this.pollConverterHelper = pollConverterHelper;
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
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
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
            Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting sending polls...");
            var poll = await pollOptionsService.GetPollOptionsAsync(DateTime.UtcNow.Date);
            var chats = await chatRepository.GetAll();
            if (poll == null)
            {
                foreach (var chat in chats)
                {
                    try
                    {
                        await botClientService.BotClient.SendTextMessageAsync(
                            chatId: chat.ChatId,
                            text:
                            "Niestety, nie posiadamy aktualnych wyników zakażeń, aby wyświetlić wyniki i udostępnić możliwość przewidywań. Kolejna próba nastąpi za 1h.",
                            cancellationToken: stoppingToken
                        );
                    }
                    catch (Exception) {}

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - Polls haven't been sent. Not enough information about covid.");
            }
            else if (!poll.ChatPollsSended)
            {
                await pollRepository.SetSendedAsync(poll.Id, true);
                poll.Options = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options);

                foreach (var chat in chats)
                {
                    try
                    {
                        var sendedPoll = await SendPoll(stoppingToken, chat.ChatId, poll.Options);
                        await pollChatRepository.AddAsync(poll.Id, new Db.PollChat()
                        {
                            ChatId = sendedPoll.Chat.Id,
                            PollId = sendedPoll.Poll.Id,
                            MessageId = sendedPoll.MessageId
                        });
                    }
                    catch (Exception) {}

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - Polls have been sent.");

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
            Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting closing polls...");
            var poll = await pollRepository.FindLatestAsync();
            if (poll == null || poll.ChatPollsClosed)
            {
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - There is nothing to close.");
                return;
            }

            var chatPolls = poll.ChatPolls;
            if (chatPolls != null)
            {
                await pollRepository.SetClosedAsync(poll.Id, true);
                foreach (var chatPoll in chatPolls)
                {
                    try
                    {
                        await StopPoll(stoppingToken, chatPoll.ChatId, chatPoll.MessageId);
                    }
                    catch (Exception) {}

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            Log.Information($"[{nameof(BotPollSenderHostedService)}] - All polls have been closed.");
        }

        private Task StopPoll(CancellationToken stoppingToken, long chatId, int messageId)
        {
            return botClientService.BotClient.StopPollAsync(chatId, messageId, cancellationToken: stoppingToken);
        }

        private Task<Message> SendPoll(CancellationToken stoppingToken, long chatId, IList<string> pollOptions)
        {
            return botClientService.BotClient.SendPollAsync(
                chatId: chatId,
                question: "Ile przewidujesz zakażeń na jutro?",
                options: pollOptions,
                isAnonymous: false,
                cancellationToken: stoppingToken
            );
        }
    }
}
