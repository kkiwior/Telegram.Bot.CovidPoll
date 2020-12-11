using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotJoinLeaveHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IChatRepository chatRepository;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollRepository pollRepository;
        private readonly IOptions<BotSettings> botOptions;
        private readonly IOptions<CovidTrackingSettings> covidTrackingOptions;

        public BotJoinLeaveHandler(BotClientService botClientService,
                                   IChatRepository chatRepository,
                                   IPollChatRankingRepository pollChatRankingRepository,
                                   IPollRepository pollRepository,
                                   IOptions<BotSettings> botOptions,
                                   IOptions<CovidTrackingSettings> covidTrackingOptions)
        {
            this.botClientService = botClientService;
            this.chatRepository = chatRepository;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.botOptions = botOptions;
            this.covidTrackingOptions = covidTrackingOptions;
        }

        public IList<BotCommand> Command => null;

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnUpdate += BotClient_OnUpdate;
        }

        private async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            if (e.Update.Message?.NewChatMembers?.Any(n => n.Id == botClientService.BotClient.BotId) == true
                || e.Update.Message?.GroupChatCreated == true
                || e.Update.Message?.SupergroupChatCreated == true)
            {
                var chat = await chatRepository.CheckExistsByIdAsync(e.Update.Message.Chat.Id);
                if (chat) 
                    return;

                await chatRepository.AddAsync(new Db.Chat { ChatId = e.Update.Message.Chat.Id });
                await pollChatRankingRepository.AddWinsCountAsync(new List<Db.PollAnswer>(), e.Update.Message.Chat.Id);
                try
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Update.Message.Chat.Id,
                        text: "<b>Informacje o bocie:</b>\n" +
                        "1. Bot ma za zadanie przewidywać ilość zakażeń w kolejnym dniu na podstawie ankiet.\n" +
                        $"2. Ankiety pojawiają się o godzinie: {botOptions.Value.PollsStartHourUtc} UTC\n" +
                        $"3. Ankiety są zamykane oraz wyświetlają się przewidywania zakażeń o godzinie: {botOptions.Value.PollsEndHourUtc} UTC\n" +
                        $"4. Aktualne zakażenia oraz ranking osób najlepiej przewidujących pojawia się o godzinie: {covidTrackingOptions.Value.FetchDataHourUtc} UTC",
                        parseMode: Types.Enums.ParseMode.Html
                    );
                }
                catch (Exception) {}
            }
            else if (e.Update.Message?.Type == Types.Enums.MessageType.ChatMemberLeft)
            {
                try
                {
                    if (e.Update.Message?.LeftChatMember?.Id == botClientService.BotClient.BotId
                        || await botClientService.BotClient.GetChatMembersCountAsync(e.Update.Message?.Chat.Id) == 1)
                    {
                        await chatRepository.DeleteByIdAsync(e.Update.Message.Chat.Id);
                    }
                }
                catch (Exception) {}
            }
        }
    }
}
