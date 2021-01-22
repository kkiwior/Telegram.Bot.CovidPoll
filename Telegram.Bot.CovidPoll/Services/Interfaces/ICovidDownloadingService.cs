using System.Threading.Tasks;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface ICovidDownloadingService
    {
        Task<bool> DownloadCovidByJsonAsync();
    }
}
