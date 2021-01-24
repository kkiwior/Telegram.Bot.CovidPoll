using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Models;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IPollVotesConverterHelper
    {
        IReadOnlyDictionary<int, int> Points { get; }
        List<PredictionsModel> ConvertPollVotes(PollChat pollChat, int? covidToday = null);
        IList<int> GetAllPossibilities(PollChat pollChat);
        Task<int?> PredictCovidCasesAsync(Poll poll);
    }
}