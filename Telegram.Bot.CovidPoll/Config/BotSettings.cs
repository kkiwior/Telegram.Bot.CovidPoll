namespace Telegram.Bot.CovidPoll.Config
{
    public class BotSettings
    {
        public string Token { get; set; }
        public int PollsStartHour { get; set; }
        public int PollsEndHour { get; set; }
        public int AdminUserId { get; set; }
    }
}
