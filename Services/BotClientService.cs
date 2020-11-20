using Microsoft.Extensions.Options;
using Telegram.Bot.CovidPoll.Config;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotClientService
    {
        public readonly ITelegramBotClient botClient;
        public BotClientService(IOptions<BotSettings> botSettings)
        {
            botClient = new TelegramBotClient(botSettings.Value.Token);
        }
    }
}
