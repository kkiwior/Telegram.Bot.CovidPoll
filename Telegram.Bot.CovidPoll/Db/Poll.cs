using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram.Bot.CovidPoll.Db
{
    public class Poll
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public List<int> Options { get; set; } = new List<int>();
        public List<PollChat> ChatPolls { get; set; } = new List<PollChat>();
        public bool ChatPollsSended { get; set; } = false;
        public bool ChatPollsClosed { get; set; } = false;
        public bool ChatPredictionsSended { get; set; } = false;
        public bool ChatPredictionsResultSended { get; set; } = false;
        public DateTime Date { get; set; }

        public PollChat FindByChatId(long chatId)
        {
            return ChatPolls.Where(cp => cp.ChatId == chatId).FirstOrDefault();
        }

        public PollChat FindByPollId(string pollId)
        {
            return ChatPolls.Where(cp => cp.PollId.Equals(pollId)).FirstOrDefault();
        }
    }
}
