using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Covid
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public int TotalCases { get; set; }
        public DateTime Date { get; set; }
    }
}
