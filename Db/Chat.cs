using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public long ChatId { get; set; }
    }
}
