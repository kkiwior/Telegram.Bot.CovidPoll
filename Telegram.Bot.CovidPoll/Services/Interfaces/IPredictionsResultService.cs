using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IPredictionsResultService
    {
        string GetAllPredictions(Poll poll, PollChat pollChat, int? covidCasesPrediction);
        Task<string> GetAllPredictionsResult(Poll poll, PollChat pollChat, int covidToday);
    }
}