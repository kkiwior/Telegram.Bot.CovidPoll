namespace Telegram.Bot.CovidPoll.Config
{
    public class BotSettings
    {
        public string Token { get; set; }
        public int PollsStartHourUtc { get; set; }
        public int PollsEndHourUtc { get; set; }
        public int AdminUserId { get; set; }
    }
}
