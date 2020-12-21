using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IChatRepository
    {
        Task AddAsync(Chat poll);
        Task DeleteByIdAsync(long id);
        Task<bool> CheckExistsByIdAsync(long id);
        Task<List<Chat>> GetAll();
    }
}
