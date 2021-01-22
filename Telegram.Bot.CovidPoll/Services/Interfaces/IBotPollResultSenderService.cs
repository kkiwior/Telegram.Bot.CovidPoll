namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IBotPollResultSenderService
    {
        void SendPredictionsToChats();
        void SendPredictionsResultsToChats();
    }
}
