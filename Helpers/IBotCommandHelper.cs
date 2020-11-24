using System.Threading.Tasks;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public interface IBotCommandHelper
    {
        Task<BotCommandModel> CheckCommandIsCorrectAsync(BotCommands commandType, string command);
    }
}
