using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Args;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Telegram.Bot.CovidPoll.Helpers.BotCommandHelper;

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotNonPollHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        private readonly IPollChatRepository pollChatRepository;
        private readonly IPollRepository pollRepository;
        private readonly IChatUserCommandRepository chatUserCommandRepository;
        private readonly BotMessageHelper botMessageHelper;
        public BotNonPollHandler(BotClientService botClientService,
                                 IBotCommandHelper botCommandHelper,
                                 IPollChatRankingRepository pollChatRankingRepository,
                                 IPollChatRepository pollChatRepository,
                                 IPollRepository pollRepository,
                                 IChatUserCommandRepository chatUserCommandRepository,
                                 BotMessageHelper botMessageHelper)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
            this.pollChatRepository = pollChatRepository;
            this.pollRepository = pollRepository;
            this.chatUserCommandRepository = chatUserCommandRepository;
            this.botMessageHelper = botMessageHelper;
        }

        public IList<BotCommand> Command =>
            new List<BotCommand>
            {
                new BotCommand
                {
                    Command = BotCommands.vote.ToString(),
                    Description = "Użycie: /vote {ilość zarażeń}."
                }
            };

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnMessage += BotClient_OnMessageVote;
            botClientService.BotClient.OnCallbackQuery += BotClient_OnCallbackQuery;
        }

        private async void BotClient_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var poll = await pollRepository.FindLatestWithoutChatsAsync();
            if (poll == null || poll.ChatPollsClosed == true)
                return;

            var pollChat = await pollChatRepository.FindLatestByChatIdAsync(e.CallbackQuery.Message.Chat.Id);
            var userLastCommand = await chatUserCommandRepository.FindAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.From.Id);
            if (userLastCommand == null)
                await chatUserCommandRepository.AddAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.From.Id, DateTime.UtcNow);

            if (pollChat == null || userLastCommand?.LastCommandDate.AddSeconds(10) >= DateTime.UtcNow)
                return;

            await chatUserCommandRepository.UpdateLastCommandAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.From.Id, DateTime.UtcNow);

            var voted = await pollChatRepository.CheckIfAlreadyVotedInNonPollAsync(e.CallbackQuery.From.Id, poll.Id, pollChat.PollId);
            if (voted)
            {
                await pollChatRepository.RemoveNonPollVoteAsync(e.CallbackQuery.From.Id, poll.Id, pollChat.PollId);
                pollChat.NonPollAnswers.Remove(pollChat.NonPollAnswers.FirstOrDefault(np => np.UserId == e.CallbackQuery.From.Id));

                await botMessageHelper.RemoveVoteFromNonPollAsync(pollChat, e.CallbackQuery.Message.Chat.Id);
            }
        }

        private async void BotClient_OnMessageVote(object sender, MessageEventArgs e)
        {
            var command = await botCommandHelper.CheckCommandIsCorrectAsync(BotCommands.vote, e.Message.Text);
            if (command.CommandCorrect && command.CommandArg != string.Empty && e.Message.Chat.Type is ChatType.Supergroup or ChatType.Group)
            {
                var poll = await pollRepository.FindLatestWithoutChatsAsync();
                if (poll == null || poll.ChatPollsClosed == true)
                    return;

                var pollChat = await pollChatRepository.FindLatestByChatIdAsync(e.Message.Chat.Id);
                var userLastCommand = await chatUserCommandRepository.FindAsync(e.Message.Chat.Id, e.Message.From.Id);
                if (userLastCommand == null)
                    await chatUserCommandRepository.AddAsync(e.Message.Chat.Id, e.Message.From.Id, DateTime.UtcNow);

                if (pollChat == null || userLastCommand?.LastCommandDate.AddSeconds(10) >= DateTime.UtcNow)
                    return;

                var votedInPoll = await pollChatRepository.CheckIfAlreadyVotedAsync(e.Message.From.Id, poll.Id, pollChat.PollId);
                if (votedInPoll)
                    return;

                await chatUserCommandRepository.UpdateLastCommandAsync(e.Message.Chat.Id, e.Message.From.Id, DateTime.UtcNow);

                var pollChatVoted = await pollChatRepository.CheckIfAlreadyVotedInPollOrNonPollAsync(e.Message.From.Id, poll.Id, pollChat.PollId);
                if (pollChatVoted == true)
                    return;

                if (int.TryParse(command.CommandArg, out var voteNumber))
                {
                    pollChat.NonPollAnswers.Add(new Db.NonPollAnswer
                    {
                        UserId = e.Message.From.Id,
                        Username = e.Message.From.Username,
                        UserFirstName = e.Message.From.FirstName,
                        VoteNumber = voteNumber
                    });

                    var messageText = new StringBuilder("<b>Lista głosów poza ankietą:</b>\n\n");
                    foreach (var vote in pollChat.NonPollAnswers.OrderBy(np => np.VoteNumber))
                    {
                        messageText.AppendLine($"{vote.Username ?? vote.UserFirstName} - {vote.VoteNumber}");
                    }
                    messageText.AppendLine(BotMessageHelper.GetNonPollMessage());
                    try
                    {
                        await botClientService.BotClient.EditMessageTextAsync(
                            chatId: e.Message.Chat.Id,
                            messageId: pollChat.NonPollMessageId,
                            text: messageText.ToString(),
                            parseMode: ParseMode.Html,
                            replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton
                            {
                                Text = "Wycofaj głos spoza ankiety",
                                CallbackData = "unvote"
                            })
                        );
                    }
                    catch (Exception) {}
                    await pollChatRepository.AddNonPollVoteAsync(e.Message.From.Id, e.Message.From.Username, e.Message.From.FirstName, poll.Id, pollChat.PollId, voteNumber);
                }
            }
        }
    }
}
