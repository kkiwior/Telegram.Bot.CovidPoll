namespace Telegram.Bot.CovidPoll.Helpers.Models
{
    public class PredictionsModel
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int VoteNumber { get; set; }
        public long Points { get; set; }

        //public override bool Equals(object obj)
        //{
        //    if (obj is PredictionsModel objParsed)
        //    {
        //        return this.UserId == objParsed.UserId &&
        //            this.Username.Equals(objParsed.Username) &&
        //            this.UserFirstName.Equals(objParsed.UserFirstName) &&
        //            this.VoteNumber == objParsed.VoteNumber &&
        //            this.Points == objParsed.Points;
        //    }
        //    return base.Equals(obj);
        //}
    }
}
