using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class BotCommandHelper : IBotCommandHelper
    {
        private readonly IBotClientService botClientService;
        public BotCommandHelper(IBotClientService botClientService)
        {
            this.botClientService = botClientService;
        }
        public enum BotCommands
        {
            start,
            stop,
            setCovid,
            test,
            ranking,
            poll,
            vote,
            unvote
        };

        public async Task<BotCommandModel> CheckCommandIsCorrectAsync(BotCommands commandType, string command)
        {
            if (command == null)
                return new BotCommandModel() {CommandCorrect = false};

            var botName = (await botClientService.BotClient.GetMeAsync()).Username;
            var regex = new Regex(@$"\G(\/{commandType}(?:@{botName})?) ?([a-zA-Z0-9]*)\Z");

            var matches = regex.Matches(command);
            if (matches.Count > 0)
            {
                if (matches[0].Groups.Count > 1)
                    return new BotCommandModel()
                    {
                        CommandCorrect = true,
                        CommandArg = matches[0].Groups[2].Value
                    };

                return new BotCommandModel()
                {
                    CommandCorrect = true
                };
            }

            return new BotCommandModel()
            {
                CommandCorrect = false
            };
        }
    }
}
