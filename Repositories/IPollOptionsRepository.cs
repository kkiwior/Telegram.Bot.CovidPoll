using System;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IPollOptionsRepository
    {
        Task AddAsync(PollOptions pollOptions);
        Task<PollOptions> GetByDateAsync(DateTime date);
    }
}
