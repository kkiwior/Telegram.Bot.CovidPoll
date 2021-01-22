using System;
namespace Telegram.Bot.CovidPoll.Exceptions
{
    public class PredictCovidCasesException : Exception
    {
        public PredictCovidCasesException() { }
        public PredictCovidCasesException(string message) : base(message) { }
        public PredictCovidCasesException(string message, Exception inner) : base(message, inner) { }
    }
}