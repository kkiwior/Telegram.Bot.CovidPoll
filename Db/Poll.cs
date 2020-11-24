using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Poll
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public List<PollChat> ChatPolls { get; set; } = new List<PollChat>();
        public bool ChatPollsSended { get; set; } = false;
        public bool ChatPollsClosed { get; set; } = false;
        public bool ChatPredictionsSended { get; set; } = false;
        public bool ChatPredictionsResultSended { get; set; } = false;
        public DateTime Date { get; set; }
    }
}
