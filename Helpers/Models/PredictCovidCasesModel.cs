namespace Telegram.Bot.CovidPoll.Helpers.Models
{
    public class PredictCovidCasesModel
    {
        public int VoteWithoutRatio { get; set; }
        public double Vote { get; set; }
        public double Ratio { get; set; }
    }
}
