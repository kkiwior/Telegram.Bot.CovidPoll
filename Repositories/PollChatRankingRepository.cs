using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using System.Linq;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class PollChatRankingRepository : IPollChatRankingRepository
    {
        private readonly MongoDb mongoDb;
        public PollChatRankingRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }
        public Task AddChatToRankingAsync(long chatId)
        {
            var chatRanking = new ChatRanking() { ChatId = chatId };
            return mongoDb.ChatsRankings.InsertOneAsync(chatRanking);
        }
        public async Task AddWinsCountAsync(IList<PollAnswer> winners, long chatId)
        {
            var ranking = await this.GetChatRankingAsync(chatId);
            if (ranking == null)
                await this.AddChatToRankingAsync(chatId);

            foreach (var winner in winners)
            {
                var user = ranking?.Winners.FirstOrDefault(w => w.UserId == winner.UserId);
                var chatWinner = new ChatWinner();
                chatWinner.UserId = winner.UserId;
                chatWinner.Username = winner.Username;
                chatWinner.UserFirstName = winner.UserFirstName;

                if (user != null)
                {
                    chatWinner.WinsCount = user.WinsCount + 1;
                    ranking.Winners[ranking.Winners.FindIndex(w => w.UserId == winner.UserId)] = chatWinner;
                }
                else
                {
                    chatWinner.WinsCount = 1;
                    ranking.Winners.Add(chatWinner);
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
    }
}
