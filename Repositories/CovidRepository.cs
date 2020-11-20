﻿using MongoDB.Driver;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class CovidRepository : ICovidRepository
    {
        private readonly MongoDb mongoDb;
        public CovidRepository(MongoDb mongoDb)
        {
            this.mongoDb = mongoDb;
        }
        public Task AddAsync(Covid covid)
        {
            return mongoDb.Covids.InsertOneAsync(covid);
        }
        public Task<Covid> FindLatestAsync()
        {
            return mongoDb.Covids.Find(_ => true).SortByDescending(c => c.Date).FirstOrDefaultAsync();
        }
    }
}