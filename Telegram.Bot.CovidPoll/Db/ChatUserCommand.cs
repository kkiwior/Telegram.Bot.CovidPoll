using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Telegram.Bot.CovidPoll.Db
{
    public class ChatUserCommand
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public DateTime LastCommandDate { get; set; } = DateTime.UtcNow;
    }
}
