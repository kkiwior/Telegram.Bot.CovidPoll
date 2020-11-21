using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class PollRepository : IPollRepository
    {
        private readonly MongoDb mongoDb;
        public PollRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }
        public Task AddAsync(Poll poll)
        {
            return mongoDb.Polls.InsertOneAsync(poll);
        }
        public Task DeleteByIdAsync(long id)
        {
            return mongoDb.Polls.DeleteOneAsync(p => p.ChatId == id);
        }
        public Task<bool> CheckExistsByIdAsync(long id)
        {
            return mongoDb.Polls.Find(p => p.ChatId == id).AnyAsync();
        }
        public Task<List<Poll>> GetAllPolls()
        {
            return mongoDb.Polls.Find(_ => true).ToListAsync();
        }
        public Task<List<Poll>> GetAllPollsByPollOptionsId(string pollOptionsId)
        {
            return mongoDb.Polls.Find(p => p.PollOptionsId.Equals(pollOptionsId)).ToListAsync();
        }
    }
}
