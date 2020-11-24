using System.Threading.Tasks;
using MongoDB.Bson;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IPollChatRepository
    {
        Task AddAsync(ObjectId pollId, PollChat pollChat);
        Task<bool> CheckIfAlreadyVotedInAllAsync(long userId, ObjectId pollId);
        Task<bool> CheckIfAlreadyVotedAsync(long userId, ObjectId pollId, string pollChatId);
        Task AddVoteAsync(long userId, string userName, ObjectId pollId, string pollTelegramId, int vote);
        Task RemoveVoteAsync(long userId, ObjectId pollId, string pollTelegramId);
    }
}
