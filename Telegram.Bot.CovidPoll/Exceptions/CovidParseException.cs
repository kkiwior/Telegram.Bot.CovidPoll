using System;

namespace Telegram.Bot.CovidPoll.Exceptions
{
    public class CovidParseException : Exception
    {
        public CovidParseException() {}
        public CovidParseException(string message) : base(message) {}
        public CovidParseException(string message, Exception inner) : base(message, inner) {}
    }
}
