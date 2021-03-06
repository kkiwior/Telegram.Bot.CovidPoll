﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Models;

namespace Telegram.Bot.CovidPoll.Repositories.Interfaces
{
    public interface IPollChatRankingRepository
    {
        Task AddWinsCountAsync(IList<PredictionsModel> winners, long chatId);
        Task<ChatRanking> GetChatRankingAsync(long chatId);
        Task UpdateLastCommandDateAsync(long chatId, DateTime date);
    }
}
