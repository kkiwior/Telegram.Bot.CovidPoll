using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram.Bot.CovidPoll.Db
{
    public class UserRatio
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public double Ratio { get; set; }
    }
}
