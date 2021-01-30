using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using System.Linq;
using System;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Models;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class PollChatRankingRepository : IPollChatRankingRepository
    {
        private readonly MongoDb mongoDb;
        private readonly IUserRatioRepository userRatioRepository;
        private readonly IPollVotesConverterHelper pollVotesConverterHelper;

        public PollChatRankingRepository(MongoDb mongoDb, IUserRatioRepository userRatioRepository,
            IPollVotesConverterHelper pollVotesConverterHelper)
        {
            this.mongoDb = mongoDb;
            this.userRatioRepository = userRatioRepository;
            this.pollVotesConverterHelper = pollVotesConverterHelper;
        }

        public Task AddChatToRankingAsync(ChatRanking chatRanking)
        {
            return mongoDb.ChatsRankings.InsertOneAsync(chatRanking);
        }

        public async Task AddWinsCountAsync(IList<PredictionsModel> winners, long chatId)
        {
            var ranking = await this.GetChatRankingAsync(chatId);
            if (ranking == null)
            {
                ranking = new ChatRanking() { ChatId = chatId };
                await this.AddChatToRankingAsync(ranking);
            }
            foreach (var winner in winners)
            {
                var user = ranking?.Winners.FirstOrDefault(w => w.UserId == winner.UserId);
                var ratio = await userRatioRepository.GetByUserIdAsync(winner.UserId, chatId);
                var chatWinner = new ChatWinner
                {
                    UserId = winner.UserId,
                    Username = winner.Username,
                    UserFirstName = winner.UserFirstName
                };
                var userRatio = new UserRatio()
                {
                    ChatId = chatId,
                    UserId = winner.UserId
                };
                if (user != null)
                {
                    if (winner.Points != 0)
                    {
                        chatWinner.WinsCount = user.WinsCount + 1;
                        chatWinner.Points = user.Points + winner.Points;
                    }
                    else
                    {
                        chatWinner.WinsCount = user.WinsCount;
                        chatWinner.Points = user.Points;
                    }
                    chatWinner.TotalVotes = user.TotalVotes + 1;
                    ranking.Winners[ranking.Winners.FindIndex(w => w.UserId == winner.UserId)] = chatWinner;
                }
                else
                {
                    if (winner.Points != 0)
                    {
                        chatWinner.WinsCount = 1;
                        chatWinner.Points = winner.Points;
                    }
                    chatWinner.TotalVotes = 1;
                    ranking.Winners.Add(chatWinner);
                }
                userRatio.Ratio = (double) chatWinner.Points / pollVotesConverterHelper.Points.Values.Max() / chatWinner.TotalVotes;
                if (ratio == null)
                {
                    await userRatioRepository.AddAsync(userRatio);
                }
                else
                {
                    await userRatioRepository
                        .UpdateAsync(userRatio.UserId, userRatio.ChatId, userRatio.Ratio);
                }
            }
            if (ranking.Winners.Count > 0)
            {
                await mongoDb.ChatsRankings.ReplaceOneAsync(c => c.ChatId == chatId, ranking);
            }
        }

        public Task<ChatRanking> GetChatRankingAsync(long chatId)
        {
            return mongoDb.ChatsRankings.Find(c => c.ChatId == chatId).FirstOrDefaultAsync();
        }

        public Task UpdateLastCommandDateAsync(long chatId, DateTime date)
        {
            return mongoDb.ChatsRankings.UpdateOneAsync(cr => cr.ChatId == chatId, 
                Builders<ChatRanking>.Update.Set(cr => cr.LastCommandDate, date));
        }
    }
}
