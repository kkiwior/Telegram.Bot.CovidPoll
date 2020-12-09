using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage chatMessage);
        Task RemoveByMessageIdAsync(int messageId);
        Task<List<ChatMessage>> GetByChatIdAsync(long chatId);
    }
}
