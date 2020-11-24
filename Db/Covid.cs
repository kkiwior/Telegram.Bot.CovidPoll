using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Covid
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public int TotalCases { get; set; }
        public int NewCases { get; set; }
        public DateTime Date { get; set; }
    }
}
