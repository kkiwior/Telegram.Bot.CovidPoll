using System.Collections.Generic;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public interface IBotCommand
    {
        void RegisterCommand(BotClientService botClient);
        IList<BotCommand> Command { get; }
    }
}
