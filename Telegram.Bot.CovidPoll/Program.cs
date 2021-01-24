using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Handlers;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.CovidPoll.Services.HostedServices;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                 {
                     services.Configure<BotSettings>(
                         hostContext.Configuration.GetSection("BotSettings"));
                     services.Configure<MongoSettings>(
                         hostContext.Configuration.GetSection("MongoSettings"));
                     services.Configure<CovidTrackingSettings>(
                         hostContext.Configuration.GetSection("CovidTrackingSettings"));

                     services.AddSingleton<MongoDb>();
                     services.AddSingleton<IBotClientService, BotClientService>();
                     services.AddSingleton<ICovidCalculateService, CovidCalculateService>();
                     services.AddHttpClient();
                     services.AddSingleton<IBotCommandHelper, BotCommandHelper>();
                     services.AddSingleton<IPollRepository, PollRepository>();
                     services.AddSingleton<ICovidRepository, CovidRepository>();
                     services.AddSingleton<IChatRepository, ChatRepository>();
                     services.AddSingleton<IPollChatRepository, PollChatRepository>();
                     services.AddSingleton<IPollChatRankingRepository, PollChatRankingRepository>();
                     services.AddSingleton<IChatMessageRepository, ChatMessageRepository>();
                     services.AddSingleton<IChatUserCommandRepository, ChatUserCommandRepository>();
                     services.AddSingleton<IUserRatioRepository, UserRatioRepository>();
                     services.AddSingleton<IBotMessageHelper, BotMessageHelper>();
                     services.AddSingleton<IPollVotesConverterHelper, PollVotesConverterHelper>();
                     services.AddSingleton<IBotEvent, BotVoteHandler>();
                     services.AddSingleton<IBotEvent, BotJoinLeaveHandler>();
                     services.AddSingleton<IBotEvent, BotAdminHandler>();
                     services.AddSingleton<IBotEvent, BotRankingHandler>();
                     services.AddSingleton<IBotEvent, BotReplyPollHandler>();
                     services.AddSingleton<IBotEvent, BotNonPollHandler>();
                     services.AddSingleton<IQueueService, QueueService>();
                     services.AddSingleton<IPollOptionsService, PollOptionsService>();
                     services.AddSingleton<IBotPollResultSenderService, BotPollResultSenderService>();
                     services.AddSingleton<ICovidDownloadingService, CovidDownloadingService>();
                     services.AddSingleton<ITaskDelayHelper, TaskDelayHelper>();
                     services.AddSingleton<IPredictionsResultService, PredictionsResultService>();
                     services.AddSingleton<IBotPollSenderService, BotPollSenderService>();
                     //services.AddHostedService<CovidTrackingHostedService>();
                     services.AddHostedService<BotEventsHostedService>();
                     services.AddHostedService<BotPollSenderHostedService>();
                     services.AddHostedService<QueueHostedService>();
                     services.AddLogging(c => c.AddSeq(hostContext.Configuration.GetSection("Seq")));
                 });
    }
}
