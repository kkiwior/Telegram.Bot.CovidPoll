using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IBotMessageHelper
    {
        Task RemoveVoteFromNonPollAsync(PollChat pollChat, ChatId chatId);
        Task<SendPollModel> SendPollAsync(ChatId chatId, IEnumerable<string> options, CancellationToken cancellationToken = default);
    }
}