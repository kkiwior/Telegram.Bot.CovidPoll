using System;
using System.Collections.Generic;
using Telegram.Bot.CovidPoll.Abstractions;

namespace Telegram.Bot.CovidPoll.Db
{
    public class PollChat
    {
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public string PollId { get; set; }
        public int NonPollMessageId { get; set; }
        public List<PollAnswer> PollAnswers { get; set; } = new List<PollAnswer>();
        public List<NonPollAnswer> NonPollAnswers { get; set; } = new List<NonPollAnswer>();
        public DateTime LastCommandDate { get; set; } = DateTime.UtcNow;
    }

    public class PollAnswer : Answer {} 

    public class NonPollAnswer : Answer {}
}
