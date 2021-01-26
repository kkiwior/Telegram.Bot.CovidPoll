using System;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class DateProvider : IDateProvider
    {
        public DateTimeOffset DateTimeOffsetUtcNow() => DateTimeOffset.UtcNow;
        public DateTime DateTimeUtcNow() => DateTime.UtcNow;
    }
}
