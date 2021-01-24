using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Handlers;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotEventsHostedService : BackgroundService
    {
        private readonly IBotClientService botClientService;
        private readonly IList<IBotEvent> botEvents;

        public BotEventsHostedService(IBotClientService botClientService, IEnumerable<IBotEvent> botEvents)
        {
            this.botClientService = botClientService;
            this.botEvents = botEvents.ToList();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await botClientService.BotClient.SetMyCommandsAsync(
                botEvents.Where(bc => bc.Command != null).SelectMany(bc => bc.Command), stoppingToken);

            foreach (var botEvent in botEvents)
            {
                botEvent.RegisterEvent(botClientService);
            }

            botClientService.BotClient.StartReceiving(cancellationToken: stoppingToken);
        }
    }
}
