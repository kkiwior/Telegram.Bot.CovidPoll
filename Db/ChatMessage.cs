using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram.Bot.CovidPoll.Db
{
    public class ChatMessage
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public ChatMessageType ChatMessageType { get; set; }
    }
    public enum ChatMessageType
    {
        Poll,
        Text
    }
}
