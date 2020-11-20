using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;

namespace Telegram.Bot.CovidPoll
{
    class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder().Build().Run();
        }
        public static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.Configure<BotSettings>(hostContext.Configuration.GetSection("BotSettings"));
                           services.Configure<MongoSettings>(hostContext.Configuration.GetSection("MongoSettings"));
                           services.Configure<CovidTrackingSettings>(hostContext.Configuration.GetSection("CovidTrackingSettings"));
                           services.AddSingleton<MongoDb>();
                           services.AddSingleton<BotClientService>();
                           services.AddSingleton<BotPollSenderHostedService>();
                           services.AddSingleton<IBotCommandHelper, BotCommandHelper>();
                           services.AddSingleton<IPollRepository, PollRepository>();
                           services.AddSingleton<ICovidRepository, CovidRepository>();
                           services.AddSingleton<IChatRepository, ChatRepository>();
                           services.AddHostedService<BotEventsHostedService>();
                           services.AddHostedService<CovidTrackingHostedService>();
                       }).UseConsoleLifetime();
        }
    }
}
