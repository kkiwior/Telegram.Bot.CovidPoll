using System.Collections.Generic;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public interface IBotEvent
    {
        void RegisterEvent(IBotClientService botClient);
        IList<BotCommand> Command { get; }
    }
}
