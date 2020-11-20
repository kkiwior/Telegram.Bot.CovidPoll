using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Handlers;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotEventsHostedService : BackgroundService
    {
        private readonly BotClientService botClientService;
        private readonly IList<IBotCommand> botCommands;
        public BotEventsHostedService(BotClientService botClientService, IEnumerable<IBotCommand> botCommands)
        {
            this.botClientService = botClientService;
            this.botCommands = botCommands.ToList();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await botClientService.BotClient.SetMyCommandsAsync(this.botCommands.SelectMany(bc => bc.Command), stoppingToken);
            foreach (var botCommand in botCommands)
            {
                botCommand.RegisterCommand(botClientService);
            }

            botClientService.BotClient.StartReceiving(cancellationToken: stoppingToken);
        }
    }
}
