using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class ChatUserCommandRepository : IChatUserCommandRepository
    {
        private readonly MongoDb mongoDb;

        public ChatUserCommandRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }

        public Task AddAsync(long chatId, long userId, DateTime date)
        {
            return mongoDb.ChatsUsersCommands.InsertOneAsync(new ChatUserCommand
            {
                ChatId = chatId,
                UserId = userId,
                LastCommandDate = date
            });
        }

        public Task<ChatUserCommand> FindAsync(long chatId, long userId)
        {
            return mongoDb.ChatsUsersCommands.Find(cuc => cuc.ChatId == chatId && cuc.UserId == userId).FirstOrDefaultAsync();
        }

        public Task UpdateLastCommandAsync(long chatId, long userId, DateTime date)
        {
            return mongoDb.ChatsUsersCommands.UpdateOneAsync(cuc => cuc.ChatId == chatId && cuc.UserId == userId,
                Builders<ChatUserCommand>.Update.Set(cuc => cuc.LastCommandDate, date));
        }
    }
}
