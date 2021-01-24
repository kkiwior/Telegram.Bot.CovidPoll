using System;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Services.Interfaces
{
    public interface IPollOptionsService
    {
        Task<Poll> GetPollOptionsAsync(DateTime date);
    }
}