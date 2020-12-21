using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollResultSenderService
    {
        private readonly QueueService queueService;
        private readonly IChatRepository chatRepository;
        private readonly BotClientService botClientService;
        private readonly IPollRepository pollRepository;
        private readonly CovidCalculateService covidCalculateService;
        private readonly IPollConverterHelper pollConverterHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly PollVotesConverterHelper pollVotesConverterHelper;
        private readonly IUserRatioRepository userRatioRepository;

        public BotPollResultSenderService(QueueService queueService,
                                          IChatRepository chatRepository,
                                          BotClientService botClientService,
                                          IPollRepository pollRepository,
                                          CovidCalculateService covidCalculateService,
                                          IPollConverterHelper pollConverterHelper,
                                          IPollChatRankingRepository pollChatRankingRepository,
                                          PollVotesConverterHelper pollVotesConverterHelper,
                                          IUserRatioRepository userRatioRepository)
        {
            this.queueService = queueService;
            this.chatRepository = chatRepository;
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
            this.covidCalculateService = covidCalculateService;
            this.pollConverterHelper = pollConverterHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollVotesConverterHelper = pollVotesConverterHelper;
            this.userRatioRepository = userRatioRepository;
        }

        public void SendPredictionsToChats()
        {
            queueService.QueueBackgroundWorkItem(async stoppingToken =>
            {
                Log.Information($"[{nameof(BotPollResultSenderService)}] - Starting sending predictions...");
                var poll = await pollRepository.FindLatestAsync();
                if (poll == null || poll.ChatPredictionsSended)
                {
                    Log.Information($"[{nameof(BotPollResultSenderService)}] - Predictions have been already sent.");
                    return;
                }
                await pollRepository.SetPredictionsSendedAsync(poll.Id, true);

                var chats = await chatRepository.GetAll();
                int? covidCasesPrediction = null;
                try
                {
                    covidCasesPrediction = await pollVotesConverterHelper.PredictCovidCasesAsync(poll);
                }
                catch (PredictCovidCasesException) {}
                foreach (var chat in chats)
                {
                    var pollChat = poll.FindByChatId(chat.ChatId);
                    if (pollChat == null)
                        continue;

                    var text = GetAllPredictions(poll, pollChat, covidCasesPrediction);
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

                    await Task.Delay(1000, stoppingToken);
                }
                Log.Information($"[{nameof(BotPollResultSenderService)}] - All predictions have been sent.");
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
                    if (cases.Date.Date != DateTime.UtcNow.Date)
                        return;

                    await pollRepository.SetPredictionsResultsSendedAsync(poll.Id, true);

                    Log.Information(
                        $"[{nameof(BotPollResultSenderService)}] - Starting sending predictions results...");
                    var chats = await chatRepository.GetAll();
                    foreach (var chat in chats)
                    {
                        var pollChat = poll.FindByChatId(chat.ChatId);
                        if (pollChat == null)
                            continue;
                        
                        var covidToday = cases.Cases;
                        var text = await GetAllPredictionsResult(poll, pollChat, covidToday);

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

                        await Task.Delay(1000, stoppingToken);
                    }

                    Log.Information(
                        $"[{nameof(BotPollResultSenderService)}] - All predictions results have been sent.");
                }
                catch (CovidCalculateException) {}
            });
        }

        private async Task<string> GetAllPredictionsResult(Poll poll, PollChat pollChat, int covidToday)
        {
            var pollOptionsText = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options, true);
            var sb = new StringBuilder($"<strong>Aktualna liczba przypadków:</strong> {covidToday:### ###}\n\n");
            if (pollChat == null || pollChat.PollAnswers.Count == 0)
            {
                sb.AppendLine("Nikt nie próbował przewidywać na tej grupie.");
            }
            else
            {
                var listOfChoices = pollChat.PollAnswers.ConvertAll(pa => poll.Options[pa.VoteId]).ToList();
                listOfChoices.AddRange(pollChat.NonPollAnswers.ConvertAll(npa => npa.VoteNumber).ToList());

                sb.AppendLine(@"Najlepiej przewidzieli:");
                var answers = pollVotesConverterHelper.ConvertPollVotes(poll, pollChat, covidToday).ToList();
                foreach (var answer in answers.Where(p => p.Points != 0).OrderByDescending(p => p.Points))
                {
                    var voteAndPoints = 
                        @$"{(answer.FromPoll ? pollOptionsText[poll.Options.IndexOf(answer.VoteNumber)] : answer.VoteNumber):### ###}
                           (+{answer.Points})";
                    if (answer.Username == null)
                        sb.AppendLine(
                            $"<a href=\"tg://user?id={answer.UserId}\">{answer.UserFirstName}</a> - zaznaczył {voteAndPoints}");
                    else
                        sb.AppendLine($"@{answer.Username} - zaznaczył {voteAndPoints}");
                }
                if (answers?.Where(p => p.Points != 0).ToList().Count == 0)
                    sb.AppendLine($"Nikt nie był w okolicach wyniku.");

                if (answers?.Count > 0)
                    await pollChatRankingRepository.AddWinsCountAsync(answers, pollChat.ChatId);

                sb.AppendLine(); sb.AppendLine("Ogólny ranking:");
                var ranking = await pollChatRankingRepository.GetChatRankingAsync(pollChat.ChatId);
                if (ranking != null)
                {
                    if (ranking.Winners.Count == 0)
                    {
                        sb.AppendLine(); sb.AppendLine("Brak osób, które poprawnie przewidziały.");
                    }

                    var usersRatio = await userRatioRepository.GetAsync(pollChat.ChatId);
                    foreach (var winner in ranking.Winners.OrderByDescending(w => w.Points).Select((value, index) => 
                             new { value, index }))
                    {
                        var userRatio = usersRatio.FirstOrDefault(ur => ur.UserId == winner.value.UserId)?.Ratio;
                        if (userRatio != null)
                            userRatio = Math.Round((double) userRatio, 2);

                        if (winner.value.Username == null)
                            sb.AppendLine(
                                $"{winner.index+1}. <a href=\"tg://user?id={winner.value.UserId}\">{winner.value.UserFirstName}</a> - {winner.value.Points} punkty/ów{(userRatio != null ? $" ({userRatio})" : "")}");
                        else
                            sb.AppendLine(
                                $"{winner.index+1}. @{winner.value.Username} - {winner.value.Points} punkty/ów{(userRatio != null ? $" ({userRatio})" : "")}");
                    }
                }
            }
            return sb.ToString();
        }

        private string GetAllPredictions(Poll poll, PollChat pollChat, int? covidCasesPrediction)
        {
            var pollOptionsText = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options, true);
            var sb = new StringBuilder("<strong>Ankiety zostały zamknięte</strong>\n");
            sb.AppendLine("Przewidywania zarażeń na kolejny dzień (ilość przypadków, około):\n");

            if (pollChat == null || (pollChat.PollAnswers.Count == 0 && pollChat.NonPollAnswers.Count == 0))
            {
                sb.AppendLine("Brak oddanych głosów na tej grupie.");
            }
            else
            {
                var answers = pollVotesConverterHelper.ConvertPollVotes(poll, pollChat).AsEnumerable()
                    .OrderByDescending(a => a.VoteNumber).ToList();
                var votes = pollVotesConverterHelper.GetAllPossibilities(poll, pollChat);
                foreach (var vote in votes)
                {
                    sb.AppendLine(
                        @$"<strong>{(vote.FromPoll ? pollOptionsText[poll.Options.IndexOf(vote.VoteNumber)] : vote.VoteNumber):### ###}
                           </strong>");
                    foreach (var answer in answers.Where(a => a.VoteNumber == vote.VoteNumber && a.FromPoll == vote.FromPoll).ToList())
                    {
                        if (answer.Username == null)
                            sb.Append($"<a href=\"tg://user?id={answer.UserId}\">{answer.UserFirstName}</a> ");
                        else
                            sb.Append($"@{answer.Username} ");
                    }
                    sb.AppendLine(); sb.AppendLine();
                }
            }
            if (covidCasesPrediction != null)
                sb.AppendLine($"Przewiduje się według wszystkich ankiet około {(covidCasesPrediction):### ###} przypadków.");
            else
                sb.AppendLine($"Nie można przewidzieć ile będzie jutro przypadków, ponieważ nikt nie głosował.");

            return sb.ToString();
        }
    }
}
