using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly MongoDb mongoDb;

        public ChatMessageRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }

        public Task AddAsync(ChatMessage chatMessage)
        {
            return mongoDb.ChatsMessages.InsertOneAsync(chatMessage);
        }

        public Task RemoveByMessageIdAsync(int messageId)
        {
            return mongoDb.ChatsMessages.DeleteOneAsync(cm => cm.MessageId == messageId);
        }

        public Task<List<ChatMessage>> GetByChatIdAsync(long chatId)
        {
            return mongoDb.ChatsMessages.Find(cm => cm.ChatId == chatId).ToListAsync();
        }
    }
}
