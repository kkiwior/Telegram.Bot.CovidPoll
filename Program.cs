using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Handlers;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;

namespace Telegram.Bot.CovidPoll
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

            Log.CloseAndFlush();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                 {
                     services.Configure<BotSettings>(hostContext.Configuration.GetSection("BotSettings"));
                     services.Configure<MongoSettings>(hostContext.Configuration.GetSection("MongoSettings"));
                     services.Configure<CovidTrackingSettings>(hostContext.Configuration.GetSection("CovidTrackingSettings"));
                     services.AddSingleton<MongoDb>();
                     services.AddSingleton<BotClientService>();
                     services.AddSingleton<BotPollSenderHostedService>();
                     services.AddHttpClient();
                     services.AddSingleton<IBotCommandHelper, BotCommandHelper>();
                     services.AddSingleton<IPollRepository, PollRepository>();
                     services.AddSingleton<ICovidRepository, CovidRepository>();
                     services.AddSingleton<IChatRepository, ChatRepository>();
                     services.AddSingleton<IBotCommand, BotStatusCommand>();
                     services.AddSingleton<IPollOptionsRepository, PollOptionsRepository>();
                     services.AddSingleton<PollOptionsService>();
                     services.AddHostedService<BotEventsHostedService>();
                     services.AddHostedService<BotPollSenderHostedService>();
                     services.AddHostedService<CovidTrackingHostedService>();

                     var seqConfiguration = hostContext.Configuration.GetSection("SerilogSettings");
                     Log.Logger = new LoggerConfiguration()
                         .WriteTo.Seq(seqConfiguration.GetSection("ServerUrl").Value, 
                                      apiKey: seqConfiguration.GetSection(("ApiKey")).Value)
                         .CreateLogger();
                 });
    }
}
