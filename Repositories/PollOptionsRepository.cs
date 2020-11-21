using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class PollOptionsRepository : IPollOptionsRepository
    {
        private readonly MongoDb mongoDb;

        public PollOptionsRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }
        public Task AddAsync(PollOptions pollOptions)
        {
            return mongoDb.PollsOptions.InsertOneAsync(pollOptions);
        }
        public Task<PollOptions> GetByDateAsync(DateTime date)
        {
            return mongoDb.PollsOptions.Find(p => p.Date == date.Date).FirstOrDefaultAsync();
        }
    }
}
