using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class PredictionsResultService : IPredictionsResultService
    {
        private readonly IPollVotesConverterHelper pollVotesConverterHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IUserRatioRepository userRatioRepository;

        public PredictionsResultService(IPollVotesConverterHelper pollVotesConverterHelper,
            IPollChatRankingRepository pollChatRankingRepository, IUserRatioRepository userRatioRepository)
        {
            this.pollVotesConverterHelper = pollVotesConverterHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.userRatioRepository = userRatioRepository;
        }

        public async Task<string> GetAllPredictionsResult(PollChat pollChat, int covidToday)
        {
            var sb = new
                StringBuilder($"<strong>Aktualna liczba przypadków:</strong> {covidToday:### ###}\n\n");

            if (pollChat == null ||
                (pollChat.PollAnswers.Count == 0 && pollChat.NonPollAnswers.Count == 0))
            {
                sb.AppendLine("Nikt nie próbował przewidywać na tej grupie.");
            }
            else
            {
                var listOfChoices = pollChat.PollAnswers.ConvertAll(pa => pa.VoteNumber)
                    .Concat(pollChat.NonPollAnswers.ConvertAll(npa => npa.VoteNumber));

                sb.AppendLine(@"Najlepiej przewidzieli:");
                var answers = pollVotesConverterHelper.ConvertPollVotes(pollChat, covidToday).ToList();

                foreach (var answer in answers
                    .Where(p => p.Points != 0)
                    .OrderByDescending(p => p.Points)
                    .ThenByDescending(p => p.VoteNumber))
                {
                    var voteAndPoints = $"{answer.VoteNumber:### ###} (+{answer.Points})";

                    if (answer.Username == null)
                    {
                        sb.AppendLine($"<a href=\"tg://user?id={answer.UserId}\">" +
                            $"{answer.UserFirstName}</a> - zaznaczył {voteAndPoints}");
                    }
                    else
                    {
                        sb.AppendLine($"@{answer.Username} - zaznaczył {voteAndPoints}");
                    }
                }
                if (answers?.Where(p => p.Points != 0).ToList().Count == 0)
                    sb.AppendLine($"Nikt nie był w okolicach wyniku.");

                if (answers?.Count > 0)
                    await pollChatRankingRepository.AddWinsCountAsync(answers, pollChat.ChatId);

                sb.AppendLine("\nOgólny ranking:");
                var ranking = await pollChatRankingRepository.GetChatRankingAsync(pollChat.ChatId);
                if (ranking != null)
                {
                    if (ranking.Winners.Count == 0)
                        sb.AppendLine("\nBrak osób, które poprawnie przewidziały.");

                    var usersRatio = await userRatioRepository.GetAsync(pollChat.ChatId);

                    foreach (var winner in ranking.Winners.OrderByDescending(w => w.Points)
                        .Select((value, index) => new { value, index }))
                    {
                        var userRatio = usersRatio
                            .FirstOrDefault(ur => ur.UserId == winner.value.UserId)?.Ratio;

                        if (userRatio != null)
                            userRatio = Math.Round((double)userRatio, 3);

                        if (winner.value.Username == null)
                        {
                            sb.AppendLine($"{winner.index + 1}. <a href=\"tg://user?id=" +
                                $"{winner.value.UserId}\">{winner.value.UserFirstName}</a>" +
                                $"- {winner.value.Points} punkty/ów" +
                                $"{(userRatio != null ? $" ({userRatio})" : "")}");
                        }
                        else
                        {
                            sb.AppendLine($"{winner.index + 1}. @{winner.value.Username} - " +
                                $"{winner.value.Points} punkty/ów{(userRatio != null ? $" ({userRatio})" : "")}");
                        }
                    }
                }
            }
            return sb.ToString();
        }

        public string GetAllPredictions(PollChat pollChat, int? covidCasesPrediction)
        {
            var sb = new StringBuilder("<strong>Ankiety zostały zamknięte</strong>\n");
            sb.AppendLine("Przewidywania zarażeń na kolejny dzień (ilość przypadków, około):\n");

            if (pollChat == null ||
                (pollChat.PollAnswers.Count == 0 && pollChat.NonPollAnswers.Count == 0))
            {
                sb.AppendLine("Brak oddanych głosów na tej grupie.");
            }
            else
            {
                var answers = pollVotesConverterHelper.ConvertPollVotes(pollChat)
                    .OrderByDescending(a => a.VoteNumber).ToList();

                var votes = pollVotesConverterHelper.GetAllPossibilities(pollChat);
                foreach (var vote in votes)
                {
                    sb.AppendLine($"<strong>{vote:### ###}</strong>");
                    foreach (var answer in answers.Where(a => a.VoteNumber == vote))
                    {
                        if (answer.Username == null)
                        {
                            sb.Append(
                                $"<a href=\"tg://user?id={answer.UserId}\">{answer.UserFirstName}</a> ");
                        }
                        else
                        {
                            sb.Append($"@{answer.Username} ");
                        }
                    }
                    sb.AppendLine("\n");
                }
            }
            if (covidCasesPrediction != null)
            {
                sb.AppendLine("Przewiduje się według wszystkich ankiet około " +
                    $"{(covidCasesPrediction):### ###} przypadków.");
            }
            else
            {
                sb.AppendLine("Nie można przewidzieć ile będzie jutro przypadków, " +
                    "ponieważ nikt nie głosował.");
            }

            return sb.ToString();
        }
    }
}
