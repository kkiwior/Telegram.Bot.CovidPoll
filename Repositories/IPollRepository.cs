using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IPollRepository
    {
        Task AddAsync(Poll poll);
        Task<Poll> GetByDateAsync(DateTime date);
        Task SetSendedAsync(ObjectId pollId, bool pollsSended);
        Task SetClosedAsync(ObjectId pollId, bool pollsClosed);
        Task<Poll> FindLatestAsync();
        Task<Poll> GetByIdAsync(ObjectId pollId);
        Task<Poll> FindByDateAsync(DateTime date);
        Task SetPredictionsResultsSendedAsync(ObjectId pollId, bool pollsPredictionsResults);
        Task SetPredictionsSendedAsync(ObjectId pollId, bool pollsPredictions);
    }
}
