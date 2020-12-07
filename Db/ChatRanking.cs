using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.Db
{
    public class ChatRanking
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public long ChatId { get; set; }
        public List<ChatWinner> Winners { get; set; } = new List<ChatWinner>();
    }
    public class ChatWinner
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int WinsCount { get; set; }
    }
}
