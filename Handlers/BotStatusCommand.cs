using System.Collections.Generic;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotStatusCommand : IBotCommand
    {
        private readonly BotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly IChatRepository chatRepository;
        public BotStatusCommand(BotClientService botClientService,
                                 IBotCommandHelper botCommandHelper,
                                 IChatRepository chatRepository)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.chatRepository = chatRepository;
        }

        public IList<BotCommand> Command =>
            new List<BotCommand>() {
                new BotCommand()
                {
                    Command = BotCommands.start.ToString(),
                    Description = "Włącza bota"
                },
                new BotCommand()
                {
                    Command = BotCommands.stop.ToString(),
                    Description = "Wyłącza bota"
                }
            };

        public void RegisterCommand(BotClientService botClient)
        {
            botClientService.BotClient.OnMessage += BotClient_OnMessage;
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            if (await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.start, e.Message.Text))
            {
                if (!await chatRepository.CheckExistsByIdAsync(e.Message.Chat.Id))
                {
                    await chatRepository.AddAsync(new Db.Chat { ChatId = e.Message.Chat.Id });
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Pomyślnie włączono bota dla tej grupy."
                    );
                }
                else
                {
                    await botClientService.BotClient.SendTextMessageAsync(
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
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Pomyślnie wyłączono bota dla tej grupy."
                    );
                }
                else
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: "Bot nie jest włączony, możesz go wystartować poprzez /start."
                    );
                }
            }
        }
    }
}
