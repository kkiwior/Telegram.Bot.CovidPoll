using System;
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
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Program exception.");
            }

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
                     services.AddSingleton<CovidCalculateService>();
                     services.AddHttpClient();
                     services.AddSingleton<IBotCommandHelper, BotCommandHelper>();
                     services.AddSingleton<IPollRepository, PollRepository>();
                     services.AddSingleton<ICovidRepository, CovidRepository>();
                     services.AddSingleton<IChatRepository, ChatRepository>();
                     services.AddSingleton<IPollChatRepository, PollChatRepository>();
                     services.AddSingleton<IPollChatRankingRepository, PollChatRankingRepository>();
                     services.AddSingleton<IChatMessageRepository, ChatMessageRepository>();
                     services.AddSingleton<IPollConverterHelper, PollConverterHelper>();
                     //services.AddSingleton<IBotEvent, BotStatusHandler>();
                     services.AddSingleton<IBotEvent, BotVoteHandler>();
                     services.AddSingleton<IBotEvent, BotJoinLeaveHandler>();
                     services.AddSingleton<IBotEvent, BotAdminHandler>();
                     services.AddSingleton<IBotEvent, BotRankingHandler>();
                     services.AddSingleton<IBotEvent, BotReplyPollHandler>();
                     services.AddSingleton<QueueService>();
                     services.AddSingleton<PollOptionsService>();
                     services.AddSingleton<BotPollResultSenderService>();
                     services.AddHostedService<CovidTrackingHostedService>();
                     services.AddHostedService<BotEventsHostedService>();
                     services.AddHostedService<BotPollSenderHostedService>();
                     services.AddHostedService<QueueHostedService>();

                     var seqConfiguration = hostContext.Configuration.GetSection("SerilogSettings");
                     Log.Logger = new LoggerConfiguration()
                         .WriteTo.Seq(seqConfiguration.GetSection("ServerUrl").Value, 
                                      apiKey: seqConfiguration.GetSection(("ApiKey")).Value)
                         .CreateLogger();
                 });
    }
}
