﻿using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.Db
{
    public class PollChat
    {
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public string PollId { get; set; }
        public List<PollAnswer> PollAnswers { get; set; } = new List<PollAnswer>();
    }
    public class PollAnswer
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public int VoteId { get; set; }
    }
}
