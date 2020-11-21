using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.CovidPoll.Config;
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
        private readonly IPollOptionsRepository pollOptionsRepository;

        public BotPollSenderHostedService(BotClientService botClientService,
                                          IOptions<BotSettings> botSettings,
                                          IChatRepository chatRepository,
                                          IPollRepository pollRepository,
                                          PollOptionsService pollOptionsService,
                                          IPollOptionsRepository pollOptionsRepository)
        {
            this.botClientService = botClientService;
            this.botSettings = botSettings;
            this.chatRepository = chatRepository;
            this.pollRepository = pollRepository;
            this.pollOptionsService = pollOptionsService;
            this.pollOptionsRepository = pollOptionsRepository;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            var pollsStart = DateTime.UtcNow.AddSeconds(5);//DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsStartHourUtc);
            var pollsEnd = DateTime.UtcNow.AddSeconds(10);//DateTime.UtcNow.Date.AddHours(botSettings.Value.PollsEndHourUtc);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= pollsStart)
                {
                    await SendPolls(stoppingToken);
                    pollsStart = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsStartHourUtc);
                }
                else if (DateTime.UtcNow >= pollsEnd)
                {
                    await StopPolls(stoppingToken);
                    pollsEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(botSettings.Value.PollsEndHourUtc);
                }
            }
        }

        private async Task SendPolls(CancellationToken stoppingToken)
        {
            Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting sending polls...");
            var pollOptions = await pollOptionsService.GetPollOptionsAsync(DateTime.UtcNow.Date);
            var chats = await chatRepository.GetAll();
            if (pollOptions == null)
            {
                foreach (var chat in chats)
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: chat.ChatId,
                        text: "Niestety nie posiadamy aktualnych wyników zakażeń, aby udostępnić możliwość przewidywań.",
                        cancellationToken: stoppingToken
                    );
                }
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - Polls haven't been sent. Not enough information about covid.");
            }
            else
            {
                pollOptions.Options = pollOptions.Options
                    .Select((o, index) => index == 0 ? $"<{o}" : o)
                    .Select((o, index) => index == pollOptions.Options.Count - 1 ? $">{o}" : o)
                    .ToList();

                foreach (var chat in chats)
                {
                    var sendedPoll = await SendPoll(stoppingToken, chat.ChatId, pollOptions.Options);
                    await pollRepository.AddAsync(new Db.Poll()
                    {
                        ChatId = sendedPoll.Chat.Id,
                        PollId = sendedPoll.Poll.Id,
                        MessageId = sendedPoll.MessageId,
                        PollOptionsId = pollOptions.Id
                    });

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - Polls have been sent.");
            }
        }

        private async Task StopPolls(CancellationToken stoppingToken)
        {
            Log.Information($"[{nameof(BotPollSenderHostedService)}] - Starting closing polls...");
            var pollOptions = await pollOptionsRepository.GetByDateAsync(DateTime.UtcNow.Date);
            if (pollOptions == null)
            {
                Log.Information($"[{nameof(BotPollSenderHostedService)}] - There is nothing to close.");
                return;
            }

            var polls = await pollRepository.GetAllPollsByPollOptionsId(pollOptions.Id);
            if (polls != null)
            {
                foreach (var poll in polls)
                {
                    await StopPoll(stoppingToken, poll.ChatId, poll.MessageId);

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
