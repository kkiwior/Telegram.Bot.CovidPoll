using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Db
{
    public class PollOptions
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public IList<string> Options { get; set; }
        public DateTime Date { get; set; }
    }
}
