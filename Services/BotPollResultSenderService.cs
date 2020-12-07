using System;
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

        public BotPollResultSenderService(QueueService queueService, IChatRepository chatRepository,
            BotClientService botClientService, IPollRepository pollRepository,
            CovidCalculateService covidCalculateService, IPollConverterHelper pollConverterHelper)
        {
            this.queueService = queueService;
            this.chatRepository = chatRepository;
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
            this.covidCalculateService = covidCalculateService;
            this.pollConverterHelper = pollConverterHelper;
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
                foreach (var chat in chats)
                {
                    var pollChat = poll.ChatPolls.FirstOrDefault(cp => cp.ChatId == chat.ChatId);
                    if (pollChat == null)
                        continue;

                    var text = GetAllPredictions(poll, pollChat);
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
                        var pollChat = poll.ChatPolls.FirstOrDefault(cp => cp.ChatId == chat.ChatId);
                        if (pollChat == null)
                            continue;
                        
                        var covidToday = cases.Cases;
                        var text = GetAllPredictionsResult(poll, pollChat, covidToday);

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

        private string GetAllPredictionsResult(Poll poll, PollChat pollChat, int covidToday)
        {
            var pollOptionsText = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options, true);
            var sb = new StringBuilder($"<strong>Aktualna liczba przypadków:</strong> {covidToday}"); sb.AppendLine(); sb.AppendLine();
            if (pollChat == null || pollChat.PollAnswers.Count == 0)
            {
                sb.AppendLine("Nikt nie próbował przewidywać na tej grupie.");
            }
            else
            {
                var listOfChoices = pollChat.PollAnswers.ConvertAll(pa => poll.Options[pa.VoteId]).ToList();
                var bestPrediction = listOfChoices.Aggregate((x, y) => Math.Abs(int.Parse(x) - covidToday) < Math.Abs(int.Parse(y) - covidToday) ? x : y);
                var indexBestPrediction = poll.Options.IndexOf(bestPrediction);

                sb.AppendLine(@"Najlepiej przewidzieli:");
                var pollAnswers = pollChat.PollAnswers.Where(pollAnswer => pollAnswer.VoteId == indexBestPrediction).ToList();
                foreach (var pollAnswer in pollAnswers)
                {
                    if (pollAnswer.Username == null)
                        sb.AppendLine($"<a href=\"tg://user?id={pollAnswer.UserId}\">{pollAnswer.UserFirstName}</a> - zaznaczył {pollOptionsText[pollAnswer.VoteId]}");
                    else
                        sb.AppendLine($"@{pollAnswer.Username} - zaznaczył {pollOptionsText[pollAnswer.VoteId]}");
                }
            }
            return sb.ToString();
        }

        private string GetAllPredictions(Poll poll, PollChat pollChat)
        {
            var pollOptionsText = pollConverterHelper.ConvertOptionsToTextOptions(poll.Options, true);
            var sb = new StringBuilder("<strong>Ankiety zostały zamknięte</strong>"); sb.AppendLine();
            sb.AppendLine("Przewidywania zarażeń na kolejny dzień (ilość przypadków, około):"); sb.AppendLine();

            if (pollChat == null || pollChat.PollAnswers.Count == 0)
            {
                sb.AppendLine("Brak oddanych głosów na tej grupie.");
            }
            else
            {
                for (var i = 0; i < pollOptionsText.Count; i++)
                {
                    sb.AppendLine($"<strong>{pollOptionsText[i]}</strong>");
                    var pollAnswers = pollChat.PollAnswers.Where(pollAnswer => pollAnswer.VoteId == i).ToList();
                    foreach (var pollAnswer in pollAnswers)
                    {
                        if (pollAnswer.Username == null)
                            sb.Append($"<a href=\"tg://user?id={pollAnswer.UserId}\">{pollAnswer.UserFirstName}</a> ");
                        else
                            sb.Append($"@{pollAnswer.Username} ");
                    }
                    if (pollAnswers.Count > 0)
                        sb.AppendLine(); 

                    sb.AppendLine();
                }
            }
            var predictionsCount = poll.ChatPolls.SelectMany(cp => cp.PollAnswers).Count();
            if (predictionsCount > 0)
            {
                //var predictions = poll.ChatPolls.SelectMany(cp => cp.PollAnswers)
                //.GroupBy(pa => pa.UserId, (pa, y) => y.First()).Sum(po => int.Parse(poll.Options[po.VoteId]));

                var predictions = poll.ChatPolls.SelectMany(cp => cp.PollAnswers)
                    .Sum(pa => int.Parse(poll.Options[pa.VoteId]));

                sb.AppendLine($"Przewiduje się według wszystkich ankiet około {predictions / predictionsCount} przypadków.");
            }

            return sb.ToString();
        }
    }
}
