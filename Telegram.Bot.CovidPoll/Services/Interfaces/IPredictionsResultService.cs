using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IPredictionsResultService
    {
        string GetAllPredictions(PollChat pollChat, int? covidCasesPrediction);
        Task<string> GetAllPredictionsResult(PollChat pollChat, int covidToday);
    }
}