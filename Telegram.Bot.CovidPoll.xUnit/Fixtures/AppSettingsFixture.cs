using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using Telegram.Bot.CovidPoll.Config;

namespace Telegram.Bot.CovidPoll.xUnit.Fixtures
{
    public class AppSettingsFixture
    {
        public IOptions<CovidTrackingSettings> CovidTrackingSettings { get; }

        public AppSettingsFixture()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Production.json", false, true)
                .Build();

            CovidTrackingSettings = Options.Create(config.GetSection("CovidTrackingSettings")
                .Get<CovidTrackingSettings>());
        }
    }
}
