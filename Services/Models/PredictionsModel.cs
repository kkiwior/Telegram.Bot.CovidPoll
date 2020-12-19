namespace Telegram.Bot.CovidPoll.Services.Models
{
    public class PredictionsModel
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int VoteNumber { get; set; }
        public long Points { get; set; }
        public bool FromPoll { get; set; }
    }
}
