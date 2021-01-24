using System;

namespace Telegram.Bot.CovidPoll.Exceptions
{
    public class CovidCalculateException : Exception
    {
        public CovidCalculateException() { }

        public CovidCalculateException(string message) : base(message) { }

        public CovidCalculateException(string message, Exception inner) : base(message, inner) { }
    }
}