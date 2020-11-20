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
            var client = new MongoClient(mongoSettings.Value.ConnetionString);
            db = client.GetDatabase(mongoSettings.Value.DbName);
        }
        public IMongoCollection<Poll> Polls => this.db.GetCollection<Poll>("polls");
        public IMongoCollection<Covid> Covids => this.db.GetCollection<Covid>("covids");
        public IMongoCollection<Chat> Chats => this.db.GetCollection<Chat>("chats");
    }
}
