using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IPollRepository
    {
        Task AddAsync(Poll poll);
        Task<bool> CheckExistsByIdAsync(long id);
        Task DeleteByIdAsync(long id);
        Task<List<Poll>> GetAllPolls();
        Task<List<Poll>> GetAllPollsByPollOptionsId(string pollOptionsId);
    }
}
