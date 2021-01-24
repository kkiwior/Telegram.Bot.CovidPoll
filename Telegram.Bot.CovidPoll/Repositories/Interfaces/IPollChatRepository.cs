using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories.Interfaces
{
    public interface IPollChatRepository
    {
        Task AddAsync(ObjectId pollId, PollChat pollChat);
        Task<bool> CheckIfAlreadyVotedInAllAsync(long userId, ObjectId pollId);
        Task<bool> CheckIfAlreadyVotedAsync(long userId, ObjectId pollId, string pollChatId);
        Task<bool> CheckIfAlreadyVotedInNonPollAsync(long userId, ObjectId pollId, string pollChatId);
        Task<bool> CheckIfAlreadyVotedInPollOrNonPollAsync(long userId, ObjectId pollId, string pollChatId);
        Task AddVoteAsync(long userId, string userName, string userFirstName, ObjectId pollId, string pollTelegramId, int vote);
        Task RemoveVoteAsync(long userId, ObjectId pollId, string pollTelegramId);
        Task<PollChat> FindLatestByChatIdAsync(long chatId);
        Task UpdateLastCommandDateAsync(long chatId, DateTime date);
        public Task AddNonPollVoteAsync(long userId, string username, string userFirstName, ObjectId pollId, string pollTelegramId, int voteNumber);
        public Task RemoveNonPollVoteAsync(long userId, ObjectId pollId, string pollTelegramId);
    }
}
