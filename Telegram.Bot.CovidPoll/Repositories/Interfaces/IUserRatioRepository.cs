using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories.Interfaces
{
    public interface IUserRatioRepository
    {
        Task AddAsync(UserRatio userRatio);
        Task UpdateAsync(long userId, long chatId, double ratio);
        Task<List<UserRatio>> GetAsync(long chatId);
        Task<UserRatio> GetByUserIdAsync(long userId, long chatId);
    }
}
