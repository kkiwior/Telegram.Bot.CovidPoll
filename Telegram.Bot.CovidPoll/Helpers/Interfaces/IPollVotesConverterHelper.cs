using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Services.Models;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IPollVotesConverterHelper
    {
        IReadOnlyDictionary<int, int> Points { get; }

        IList<PredictionsModel> ConvertPollVotes(Poll poll, PollChat pollChat, int? covidToday = null);
        IList<PossibilitiesModel> GetAllPossibilities(Poll poll, PollChat pollChat);
        Task<int> PredictCovidCasesAsync(Poll poll);
    }
}