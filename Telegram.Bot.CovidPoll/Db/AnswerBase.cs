namespace Telegram.Bot.CovidPoll.Db
{
    public class AnswerBase
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int VoteNumber { get; set; }
    }
}
