using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class UserRatioRepository : IUserRatioRepository
    {
        private readonly MongoDb mongoDb;

        public UserRatioRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }

        public Task AddAsync(UserRatio userRatio)
        {
            return mongoDb.UsersRatio.InsertOneAsync(userRatio);
        }

        public Task UpdateAsync(long userId, long chatId, double ratio)
        {
            return mongoDb.UsersRatio.UpdateOneAsync(ur => ur.UserId == userId && ur.ChatId == chatId, 
                Builders<UserRatio>.Update.Set(ur => ur.Ratio, ratio));
        }

        public Task<List<UserRatio>> GetAsync(long chatId)
        {
            return mongoDb.UsersRatio.Find(ur => ur.ChatId == chatId).ToListAsync();
        }

        public Task<UserRatio> GetByUserIdAsync(long userId, long chatId)
        {
            return mongoDb.UsersRatio.Find(ur => ur.ChatId == chatId && ur.UserId == userId).FirstOrDefaultAsync();
        }
    }
}
