using System;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IChatUserCommandRepository
    {
        Task AddAsync(long chatId, long userId, DateTime date);
        Task<ChatUserCommand> FindAsync(long chatId, long userId);
        Task UpdateLastCommandAsync(long chatId, long userId, DateTime date);
    }
}
