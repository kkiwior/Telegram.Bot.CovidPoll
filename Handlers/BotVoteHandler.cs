using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
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
        private readonly BotMessageHelper botMessageHelper;

        public BotVoteHandler(BotClientService botClientService,
                              IPollRepository pollRepository,
                              IPollChatRepository pollChatRepository,
                              IChatRepository chatRepository,
                              BotMessageHelper botMessageHelper)
        {
            this.botClientService = botClientService;
            this.pollRepository = pollRepository;
            this.pollChatRepository = pollChatRepository;
            this.chatRepository = chatRepository;
            this.botMessageHelper = botMessageHelper;
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

            var latestPoll = await pollRepository.FindLatestWithoutChatsAsync();
            if (latestPoll != null)
            {
                var pollChat = await pollChatRepository.FindLatestByPollIdAsync(e.Update.PollAnswer.PollId);
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
                    var alreadyVoted = await pollChatRepository.CheckIfAlreadyVotedAsync(e.Update.PollAnswer.User.Id, latestPoll.Id, pollChat.PollId);
                    if (!alreadyVoted)
                    {
                        await pollChatRepository.AddVoteAsync(e.Update.PollAnswer.User.Id, e.Update.PollAnswer.User.Username, e.Update.PollAnswer.User.FirstName, latestPoll.Id,
                            e.Update.PollAnswer.PollId, e.Update.PollAnswer.OptionIds[0]);

                        var alreadyVotedInNonPoll = await pollChatRepository.CheckIfAlreadyVotedInNonPollAsync(e.Update.PollAnswer.User.Id, latestPoll.Id, pollChat.PollId);
                        if (alreadyVotedInNonPoll)
                        {
                            await pollChatRepository.RemoveNonPollVoteAsync(e.Update.PollAnswer.User.Id, latestPoll.Id, pollChat.PollId);
                            pollChat.NonPollAnswers.Remove(pollChat.NonPollAnswers.FirstOrDefault(np => np.UserId == e.Update.PollAnswer.User.Id));

                            await botMessageHelper.RemoveVoteFromNonPollAsync(pollChat, pollChat.ChatId);
                        }
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
