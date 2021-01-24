using Microsoft.Extensions.Options;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotClientService : IBotClientService
    {
        public BotClientService(IOptions<BotSettings> botSettings)
        {
            BotClient = new TelegramBotClient(botSettings.Value.Token);
        }

        public ITelegramBotClient BotClient { get; private set; }
    }
}
