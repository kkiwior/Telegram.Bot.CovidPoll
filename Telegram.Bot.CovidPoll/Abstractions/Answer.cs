namespace Telegram.Bot.CovidPoll.Abstractions
{
    public abstract class Answer
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int VoteNumber { get; set; }
    }
}
