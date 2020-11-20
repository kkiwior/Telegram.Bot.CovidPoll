using System.Threading.Tasks;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public interface IBotCommandHelper
    {
        Task<bool> CheckCommandIsCorrectAsync(BotCommands commandType, string command);
    }
}
