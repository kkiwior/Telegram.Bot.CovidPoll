using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Db;

namespace Telegram.Bot.CovidPoll.xUnit.Integration.Fixtures
{
    public class MongoFixture
    {
        public MongoDb MongoDbContext { get; }

        public MongoFixture()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetSection("ConnectionString").Value;
            var dbName = config.GetSection("DbName").Value;

            if (connectionString is null || dbName is null)
                throw new NullReferenceException();

            var options = Options.Create(new MongoSettings()
            {
                ConnectionString = connectionString,
                DbName = dbName
            });

            this.MongoDbContext = new MongoDb(options);
        }
    }
}
