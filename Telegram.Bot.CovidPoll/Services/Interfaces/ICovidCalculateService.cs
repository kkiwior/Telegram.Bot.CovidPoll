using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Services.Models;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface ICovidCalculateService
    {
        Task<CovidCasesModel> GetActualNumberOfCasesAsync();
    }
}