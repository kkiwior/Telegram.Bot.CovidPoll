using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Poll
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public string PollId { get; set; }
        public string PollOptionsId { get; set; }
        public IList<PollAnswer> PollAnswers { get; set; }
    }
}
