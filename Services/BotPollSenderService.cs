using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Repositories;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotPollSenderService
    {
        private readonly BotClientService botClientService;
        private readonly IPollRepository pollRepository;
        public BotPollSenderService(BotClientService botClientService, IPollRepository pollRepository)
        {
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
        }
        public Task SendPollsAsync()
        {
            return Task.CompletedTask;
        }
    }
}
