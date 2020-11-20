using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Services;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class BotCommandHelper : IBotCommandHelper
    {
        private readonly BotClientService botClientService;
        public BotCommandHelper(BotClientService botClientService)
        {
            this.botClientService = botClientService;
        }
        public enum BotCommands
        {
            start,
            stop
        };
        public async Task<bool> CheckCommandIsCorrectAsync(BotCommands commandType, string command)
        {
            if (command == null)
                return false;

            var botName = (await botClientService.BotClient.GetMeAsync()).Username;
            var regex = new Regex(@$"\G\/{commandType}(?:@{botName})?\Z");

            return regex.IsMatch(command.ToString());
        }
    }
}
