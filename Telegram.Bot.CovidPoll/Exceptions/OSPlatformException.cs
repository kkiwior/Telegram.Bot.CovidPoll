using System;

namespace Telegram.Bot.CovidPoll.Exceptions
{
    public class OSPlatformException : Exception
    {
        public OSPlatformException() { }

        public OSPlatformException(string message) : base(message) { }
    }
}
