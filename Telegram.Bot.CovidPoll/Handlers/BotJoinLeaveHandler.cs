using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.Services.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotJoinLeaveHandler : IBotEvent
    {
        private readonly IBotClientService botClientService;
        private readonly IChatRepository chatRepository;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollRepository pollRepository;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IOptions<BotSettings> botOptions;
        private readonly IOptions<CovidTrackingSettings> covidTrackingOptions;
        private readonly IBotMessageHelper botMessageHelper;

        public BotJoinLeaveHandler(IBotClientService botClientService, IChatRepository chatRepository, 
            IPollChatRankingRepository pollChatRankingRepository, IPollRepository pollRepository, 
            IPollChatRepository pollChatRepository, IOptions<BotSettings> botOptions, 
            IOptions<CovidTrackingSettings> covidTrackingOptions, IBotMessageHelper botMessageHelper)
        {
            this.botClientService = botClientService;
            this.chatRepository = chatRepository;
            this.pollChatRepository = pollChatRepository;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollRepository = pollRepository;
            this.botOptions = botOptions;
            this.covidTrackingOptions = covidTrackingOptions;
            this.botMessageHelper = botMessageHelper;
        }

        public IList<BotCommand> Command => null;

        public void RegisterEvent(IBotClientService botClient)
        {
            botClient.BotClient.OnUpdate += BotClient_OnUpdate;
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
                await pollChatRankingRepository
                    .AddWinsCountAsync(new List<PredictionsModel>(), e.Update.Message.Chat.Id);

                try
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Update.Message.Chat.Id,
                        text: BotMessageHelper.GetBotJoinMessage(botOptions.Value.PollsStartHour,
                            botOptions.Value.PollsEndHour, covidTrackingOptions.Value.FetchDataHour),
                        parseMode: Types.Enums.ParseMode.Html
                    );

                    var latestPoll = await pollRepository.FindLatestAsync();
                    if (latestPoll?.ChatPollsSended == true && latestPoll?.ChatPollsClosed == false)
                    {
                        var pollChat = latestPoll.FindByChatId(e.Update.Message.Chat.Id);
                        if (pollChat != null)
                            return;

                        var convertedPollOptions = latestPoll.Options
                            .Select(o => o.ToString("### ###")).ToList();

                        var sendedPoll = await botMessageHelper
                            .SendPollAsync(e.Update.Message.Chat.Id, convertedPollOptions);

                        await pollChatRepository.AddAsync(latestPoll.Id, new Db.PollChat()
                        {
                            ChatId = e.Update.Message.Chat.Id,
                            PollId = sendedPoll.PollMessage.Poll.Id,
                            MessageId = sendedPoll.PollMessage.MessageId,
                            NonPollMessageId = sendedPoll.NonPollMessage.MessageId
                        });
                    }
                }
                catch (Exception) {}
            }
            else if (e.Update.Message?.Type == Types.Enums.MessageType.ChatMemberLeft)
            {
                try
                {
                    if (e.Update.Message?.LeftChatMember?.Id == botClientService.BotClient.BotId || 
                        await botClientService.BotClient.GetChatMembersCountAsync(e.Update.Message?.Chat.Id) == 1)
                    {
                        await chatRepository.DeleteByIdAsync(e.Update.Message.Chat.Id);
                    }
                }
                catch (Exception) {}
            }
        }
    }
}
