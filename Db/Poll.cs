using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
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
        public IList<string> PollOptions { get; set; }
        public IList<PollAnswer> PollAnswers { get; set; }
        public DateTime Date { get; set; }
    }
}
