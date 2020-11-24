using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotJoinLeaveHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IPollRepository pollRepository;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IChatRepository chatRepository;

        public BotJoinLeaveHandler(BotClientService botClientService, IPollRepository pollRepository,
            IPollChatRepository pollChatRepository, IChatRepository chatRepository)
        {
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
            this.pollChatRepository = pollChatRepository;
            this.chatRepository = chatRepository;
        }

        public IList<BotCommand> Command => null;

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnUpdate += BotClient_OnUpdate;
        }

        private async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            if (e.Update.Message?.NewChatMembers?.Any(n => n.Id == botClientService.BotClient.BotId) == true)
            {
                var chat = await chatRepository.CheckExistsByIdAsync(e.Update.Message.Chat.Id);
                if (chat) 
                    return;

                await chatRepository.AddAsync(new Db.Chat { ChatId = e.Update.Message.Chat.Id });
                await botClientService.BotClient.SendTextMessageAsync(
                    chatId: e.Update.Message.Chat.Id,
                    text: "Pomyślnie włączono bota dla tej grupy."
                );
            }
            else if (e.Update.Message?.LeftChatMember?.Id == botClientService.BotClient.BotId)
            {
                await chatRepository.DeleteByIdAsync(e.Update.Message.Chat.Id);
            }
        }
    }
}
