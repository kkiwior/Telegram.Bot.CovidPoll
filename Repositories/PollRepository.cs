using System;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;
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
        public Task<Poll> GetByDateAsync(DateTime date)
        {
            return mongoDb.Polls.Find(p => p.Date == date.Date).FirstOrDefaultAsync();
        }
        public Task SetSendedAsync(ObjectId pollId, bool pollsSended)
        {
            return mongoDb.Polls.UpdateOneAsync(po => po.Id == pollId,
                Builders<Poll>.Update.Set(po => po.ChatPollsSended, pollsSended));
        }
        public Task SetPredictionsSendedAsync(ObjectId pollId, bool pollsPredictions)
        {
            return mongoDb.Polls.UpdateOneAsync(po => po.Id == pollId,
                Builders<Poll>.Update.Set(po => po.ChatPredictionsSended, pollsPredictions));
        }
        public Task SetPredictionsResultsSendedAsync(ObjectId pollId, bool pollsPredictionsResults)
        {
            return mongoDb.Polls.UpdateOneAsync(po => po.Id == pollId,
                Builders<Poll>.Update.Set(po => po.ChatPredictionsResultSended, pollsPredictionsResults));
        }
        public Task SetClosedAsync(ObjectId pollId, bool pollsClosed)
        {
            return mongoDb.Polls.UpdateOneAsync(po => po.Id == pollId,
                Builders<Poll>.Update.Set(po => po.ChatPollsClosed, pollsClosed));
        }
        public Task<Poll> FindLatestAsync()
        {
            return mongoDb.Polls.Find(_ => true).SortByDescending(p => p.Date).FirstOrDefaultAsync();
        }
        public Task<Poll> FindByDateAsync(DateTime date)
        {
            return mongoDb.Polls.Find(p => p.Date == date.Date).FirstOrDefaultAsync();
        }
        public Task<Poll> GetByIdAsync(ObjectId pollId)
        {
            return mongoDb.Polls.Find(p => p.Id == pollId).FirstOrDefaultAsync();
        }
    }
}
