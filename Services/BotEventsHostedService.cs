using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Services
{
    public class BotEventsHostedService : BackgroundService
    {
        private readonly BotClientService botClientService;
        private readonly IChatRepository chatRepository;
        private readonly IBotCommandHelper botCommandHelper;
        public BotEventsHostedService(BotClientService botClientService, IChatRepository chatRepository, IBotCommandHelper botCommandHelper)
        {
            this.botClientService = botClientService;
            this.chatRepository = chatRepository;
            this.botCommandHelper = botCommandHelper;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            botClientService.botClient.OnMessage += Bot_OnMessage;
            botClientService.botClient.OnUpdate += Bot_OnUpdate;

            botClientService.botClient.StartReceiving();

            return Task.CompletedTask;
        }

        private async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.start, e.Message.Text))
            {
                if (!await chatRepository.CheckExistsByIdAsync(e.Message.Chat.Id))
                {
                    await chatRepository.AddAsync(new Db.Chat { ChatId = e.Message.Chat.Id });
                    await botClientService.botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Pomyślnie włączono bota dla tej grupy."
                    );
                }
                else
                {
                    await botClientService.botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Bot jest już włączony."
                    );
                }
            }
            else if (await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.stop, e.Message.Text))
            {
                if (await chatRepository.CheckExistsByIdAsync(e.Message.Chat.Id))
                {
                    await chatRepository.DeleteByIdAsync(e.Message.Chat.Id);
                    await botClientService.botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Pomyślnie wyłączono bota dla tej grupy."
                    );
                }
                else
                {
                    await botClientService.botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Bot nie jest włączony, możesz go wystartować poprzez /start."
                    );
                }
            }
        }
        private void Bot_OnUpdate(object sender, UpdateEventArgs e)
        {
            var test = e.Update.PollAnswer?.User;
            var test2 = e.Update.Poll?.ExplanationEntities;
        }
        //Message pollMessage = await botClientService.botClient.SendPollAsync(
        //    chatId: e.Message.Chat.Id,
        //    question: "Pytanie",
        //    options: new[]
        //    {
        //        "Test",
        //        "test2"
        //    },
        //    isAnonymous: false
        //);
        //await Task.Delay(3000);
        //var poll = await botClientService.botClient.StopPollAsync(
        //    chatId: e.Message.Chat.Id,
        //    messageId: pollMessage.MessageId
        //);
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            botClientService.botClient.StopReceiving();

            return base.StopAsync(cancellationToken);
        }
    }
}
