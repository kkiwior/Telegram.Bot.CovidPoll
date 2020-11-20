using Microsoft.Extensions.Options;
using Telegram.Bot.CovidPoll.Config;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotClientService
    {
        public BotClientService(IOptions<BotSettings> botSettings)
        {
            BotClient = new TelegramBotClient(botSettings.Value.Token);
        }
        public ITelegramBotClient BotClient { get; private set; }
    }
}
