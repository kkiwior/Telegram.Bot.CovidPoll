using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotVoteHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IPollRepository pollRepository;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IChatRepository chatRepository;

        public BotVoteHandler(BotClientService botClientService, IPollRepository pollRepository, IPollChatRepository pollChatRepository, IChatRepository chatRepository)
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
            if (e.Update.PollAnswer == null)
                return;

            var latestPoll = await pollRepository.FindLatestAsync();
            if (latestPoll != null)
            {
                var pollChat = latestPoll.ChatPolls.FirstOrDefault(cp => cp.PollId.Equals(e.Update.PollAnswer.PollId));
                if (pollChat == null)
                    return;

                var chatAvailable = await chatRepository.CheckExistsByIdAsync(pollChat.ChatId);
                if (!chatAvailable)
                    return;

                if (latestPoll.ChatPollsClosed)
                {
                    try
                    {
                        await botClientService.BotClient.StopPollAsync(pollChat.ChatId, pollChat.MessageId);
                        await botClientService.BotClient.SendTextMessageAsync(
                            chatId: pollChat.ChatId,
                            text: "Przewidywania zostały już zamknięte, głos się nie liczy."
                        );
                    }
                    catch {}

                    return;
                }
                if (e.Update.PollAnswer.OptionIds.Length > 0)
                {
                    var alreadyVoted = await pollChatRepository.CheckIfAlreadyVotedAsync(e.Update.PollAnswer.User.Id,
                        latestPoll.Id, pollChat.PollId);
                    if (!alreadyVoted)
                    {
                        await pollChatRepository.AddVoteAsync(e.Update.PollAnswer.User.Id, e.Update.PollAnswer.User.Username, e.Update.PollAnswer.User.FirstName, latestPoll.Id,
                            e.Update.PollAnswer.PollId, e.Update.PollAnswer.OptionIds[0]);
                    }
                }
                else
                {
                    await pollChatRepository.RemoveVoteAsync(e.Update.PollAnswer.User.Id, latestPoll.Id,
                        e.Update.PollAnswer.PollId);
                }
            }
        }
    }
}
