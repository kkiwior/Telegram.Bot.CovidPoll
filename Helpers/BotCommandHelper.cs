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
            stop,
            setCovid
        };

        public async Task<BotCommandModel> CheckCommandIsCorrectAsync(BotCommands commandType, string command)
        {
            if (command == null)
                return new BotCommandModel() {CommandCorrect = false};

            var botName = (await botClientService.BotClient.GetMeAsync()).Username;
            var regex = new Regex(@$"\G(\/{commandType}(?:@CovidPollBot)?) ?([a-zA-Z0-9]*)\Z");

            var matches = regex.Matches(command);
            if (matches.Count > 0)
            {
                return new BotCommandModel()
                {
                    CommandCorrect = true,
                    CommandArg = matches[0].Groups[2].Value
                };
            }

            return new BotCommandModel()
            {
                CommandCorrect = false
            };
        }
    }
}
