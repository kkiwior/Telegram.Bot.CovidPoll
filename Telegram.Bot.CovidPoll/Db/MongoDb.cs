using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telegram.Bot.CovidPoll.Config;

namespace Telegram.Bot.CovidPoll.Db
{
    public class MongoDb
    {
        private readonly IMongoDatabase db;

        public MongoDb(IOptions<MongoSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            db = client.GetDatabase(mongoSettings.Value.DbName);
        }

        public IMongoCollection<Covid> Covids => this.db.GetCollection<Covid>("covids");

        public IMongoCollection<Chat> Chats => this.db.GetCollection<Chat>("chats");

        public IMongoCollection<Poll> Polls => this.db.GetCollection<Poll>("polls");

        public IMongoCollection<ChatRanking> ChatsRankings => 
            this.db.GetCollection<ChatRanking>("chatsrankings");

        public IMongoCollection<ChatMessage> ChatsMessages => 
            this.db.GetCollection<ChatMessage>("chatsmessages");

        public IMongoCollection<ChatUserCommand> ChatsUsersCommands => 
            this.db.GetCollection<ChatUserCommand>("chatsuserscommands");

        public IMongoCollection<UserRatio> UsersRatio => this.db.GetCollection<UserRatio>("usersratio");
    }
}
