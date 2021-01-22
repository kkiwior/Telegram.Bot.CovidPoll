using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface ICovidRepository
    {
        Task AddAsync(Covid covid);
        Task<Covid> FindLatestAsync();
        Task<List<Covid>> FindLatestLimitAsync(int limit);
    }
}
