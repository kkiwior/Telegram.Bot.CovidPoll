using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Helpers.Models;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IBotCommandHelper
    {
        Task<BotCommandModel> CheckCommandIsCorrectAsync(BotCommands commandType, string command);
    }
}
