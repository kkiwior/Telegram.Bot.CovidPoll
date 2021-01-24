namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IBotClientService
    {
        ITelegramBotClient BotClient { get; }
    }
}