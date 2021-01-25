using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollSenderService : IBotPollSenderService
    {
        private readonly IBotClientService botClientService;
        private readonly IChatRepository chatRepository;
        private readonly IPollRepository pollRepository;
        private readonly IPollOptionsService pollOptionsService;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IQueueService queueService;
        private readonly IBotMessageHelper botMessageHelper;
        private readonly ILogger<BotPollSenderService> log;
        private readonly ITaskDelayProvider taskDelayProvider;

        public BotPollSenderService(IBotClientService botClientService,
            IChatRepository chatRepository, IPollRepository pollRepository,
            IPollOptionsService pollOptionsService, IPollChatRepository pollChatRepository,
            IQueueService queueService, IBotMessageHelper botMessageHelper, 
            ILogger<BotPollSenderService> log, ITaskDelayProvider taskDelayProvider)
        {
            this.botClientService = botClientService;
            this.chatRepository = chatRepository;
            this.pollRepository = pollRepository;
            this.pollOptionsService = pollOptionsService;
            this.pollChatRepository = pollChatRepository;
            this.queueService = queueService;
            this.botMessageHelper = botMessageHelper;
            this.log = log;
            this.taskDelayProvider = taskDelayProvider;
        }

        public async Task<bool> SendPolls(CancellationToken stoppingToken)
        {
            log.LogInformation($"[{nameof(BotPollSenderService)}] - Starting sending polls...");
            var poll = await pollOptionsService.GetPollOptionsAsync(DateTime.UtcNow);
            var chats = await chatRepository.GetAll();

            if (poll?.ChatPollsSended == false)
            {
                await pollRepository.SetSendedAsync(poll.Id, true);
                var convertedPollOptions = poll.Options.ConvertAll(o => o.ToString("### ###"));

                queueService.QueueBackgroundWorkItem(async stoppingToken =>
                {
                    foreach (var chat in chats)
                    {
                        try
                        {
                            var sendedPoll =
                                await SendPoll(chat.ChatId, convertedPollOptions, stoppingToken);

                            await pollChatRepository.AddAsync(poll.Id, new PollChat()
                            {
                                ChatId = sendedPoll.PollMessage.Chat.Id,
                                PollId = sendedPoll.PollMessage.Poll.Id,
                                MessageId = sendedPoll.PollMessage.MessageId,
                                NonPollMessageId = sendedPoll.NonPollMessage.MessageId
                            });
                        }
                        catch (Exception) { }

                        await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                });
                log.LogInformation($"[{nameof(BotPollSenderService)}] - Polls have been sent.");

                return true;
            }
            else if (poll?.ChatPollsSended == true)
            {
                log.LogInformation($"[{nameof(BotPollSenderService)}] - Polls have been already sent.");
                return true;
            }
            log.LogInformation($"[{nameof(BotPollSenderService)}] - Polls don't exist.");
            return false;
        }

        public async Task StopPolls(CancellationToken stoppingToken)
        {
            log.LogInformation($"[{nameof(BotPollSenderService)}] - Starting closing polls...");
            var poll = await pollRepository.FindLatestAsync();
            if (poll == null || poll.ChatPollsClosed)
            {
                log.LogInformation($"[{nameof(BotPollSenderService)}] - There is nothing to close.");
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

                        await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                });
            }
            log.LogInformation($"[{nameof(BotPollSenderService)}] - All polls have been closed.");
        }

        private Task StopPoll(long chatId, int messageId, CancellationToken stoppingToken)
        {
            return botClientService.BotClient
                .StopPollAsync(chatId, messageId, cancellationToken: stoppingToken);
        }

        private Task<SendPollModel> SendPoll(long chatId, IList<string> pollOptions,
            CancellationToken stoppingToken)
        {
            return botMessageHelper.SendPollAsync(chatId, pollOptions, stoppingToken);
        }
    }
}
