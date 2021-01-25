using Microsoft.Extensions.Logging;
using System;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollResultSenderService : IBotPollResultSenderService
    {
        private readonly IQueueService queueService;
        private readonly IChatRepository chatRepository;
        private readonly IBotClientService botClientService;
        private readonly IPollRepository pollRepository;
        private readonly ICovidCalculateService covidCalculateService;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollVotesConverterHelper pollVotesConverterHelper;
        private readonly IUserRatioRepository userRatioRepository;
        private readonly ILogger<BotPollResultSenderService> log;
        private readonly IPredictionsResultService predictionsResultService;
        private readonly ITaskDelayProvider taskDelayProvider;

        public BotPollResultSenderService(IQueueService queueService, IChatRepository chatRepository, 
            IBotClientService botClientService, IPollRepository pollRepository, 
            ICovidCalculateService covidCalculateService, 
            IPollChatRankingRepository pollChatRankingRepository, 
            IPollVotesConverterHelper pollVotesConverterHelper, IUserRatioRepository userRatioRepository,
            ILogger<BotPollResultSenderService> log, IPredictionsResultService predictionsResultService,
            ITaskDelayProvider taskDelayProvider)
        {
            this.queueService = queueService;
            this.chatRepository = chatRepository;
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
            this.covidCalculateService = covidCalculateService;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollVotesConverterHelper = pollVotesConverterHelper;
            this.userRatioRepository = userRatioRepository;
            this.log = log;
            this.predictionsResultService = predictionsResultService;
            this.taskDelayProvider = taskDelayProvider;
        }

        public void SendPredictionsToChats()
        {
            queueService.QueueBackgroundWorkItem(async stoppingToken =>
            {
                log.LogInformation(
                    $"[{nameof(BotPollResultSenderService)}] - Starting sending predictions...");

                var poll = await pollRepository.FindLatestAsync();
                if (poll == null || poll.ChatPredictionsSended)
                {
                    log.LogInformation(
                        $"[{nameof(BotPollResultSenderService)}] - Predictions have been already sent.");
                    return;
                }
                await pollRepository.SetPredictionsSendedAsync(poll.Id, true);

                var chats = await chatRepository.GetAll();
                var covidCasesPrediction = await pollVotesConverterHelper.PredictCovidCasesAsync(poll);
                foreach (var chat in chats)
                {
                    var pollChat = poll.FindByChatId(chat.ChatId);
                    if (pollChat == null)
                        continue;

                    var text = 
                        predictionsResultService.GetAllPredictions(pollChat, covidCasesPrediction);
                    try
                    {
                        await botClientService.BotClient.SendTextMessageAsync(
                            chatId: chat.ChatId,
                            text: text,
                            parseMode: ParseMode.Html,
                            cancellationToken: stoppingToken
                        );
                    }
                    catch (Exception) {}

                    await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                log.LogInformation(
                    $"[{nameof(BotPollResultSenderService)}] - All predictions have been sent.");
            });
        }

        public void SendPredictionsResultsToChats()
        {
            queueService.QueueBackgroundWorkItem(async stoppingToken =>
            {
                var poll = await pollRepository.FindLatestAsync();
                if (poll == null || poll.ChatPredictionsResultSended || !poll.ChatPollsClosed)
                    return;

                try
                {
                    var cases = await covidCalculateService.GetActualNumberOfCasesAsync();
                    if (cases.Date.Date != DateTime.UtcNow.Date || 
                        poll.Date.Date != DateTime.UtcNow.Date.AddDays(-1))
                        return;

                    await pollRepository.SetPredictionsResultsSendedAsync(poll.Id, true);

                    log.LogInformation(
                        $"[{nameof(BotPollResultSenderService)}] - Starting sending predictions results...");

                    var chats = await chatRepository.GetAll();
                    foreach (var chat in chats)
                    {
                        var pollChat = poll.FindByChatId(chat.ChatId);
                        if (pollChat == null)
                            continue;
                        
                        var covidToday = cases.Cases;
                        var text = await predictionsResultService
                            .GetAllPredictionsResult(pollChat, covidToday);

                        try
                        {
                            await botClientService.BotClient.SendTextMessageAsync(
                                chatId: chat.ChatId,
                                text: text,
                                parseMode: ParseMode.Html,
                                cancellationToken: stoppingToken
                            );
                        }
                        catch (Exception) {}

                        await taskDelayProvider.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }

                    log.LogInformation(
                        $"[{nameof(BotPollResultSenderService)}] - All predictions results have been sent.");
                }
                catch (CovidCalculateException) {}
            });
        }
    }
}
