using System;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IDateProvider
    {
        DateTimeOffset DateTimeOffsetUtcNow();
    }
}