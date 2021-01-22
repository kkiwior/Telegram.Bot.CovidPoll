using System.Collections.Generic;
using MongoDB.Driver;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly MongoDb mongoDb;
        public ChatRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }
        public Task AddAsync(Chat poll)
        {
            return mongoDb.Chats.InsertOneAsync(poll);
        }
        public Task DeleteByIdAsync(long id)
        {
            return mongoDb.Chats.DeleteOneAsync(p => p.ChatId == id);
        }
        public Task<bool> CheckExistsByIdAsync(long id)
        {
            return mongoDb.Chats.Find(p => p.ChatId == id).AnyAsync();
        }
        public Task<List<Chat>> GetAll()
        {
            return mongoDb.Chats.Find(_ => true).ToListAsync();
        }
    }
}
