using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class BotMessageHelper
    {
        private readonly BotClientService botClientService;

        public BotMessageHelper(BotClientService botClientService)
        {
            this.botClientService = botClientService;
        }

        public async Task<SendPollModel> SendPollAsync(ChatId chatId,
                                                       IEnumerable<string> options,
                                                       CancellationToken cancellationToken = default)
        {
            var poll = await botClientService.BotClient.SendPollAsync(
                chatId: chatId,
                question: "Ile przewidujesz zakażeń na jutro?",
                options: options,
                isAnonymous: false,
                cancellationToken: cancellationToken
            );
            var nonPoll = await botClientService.BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: "<b>Lista głosów poza ankietą:</b>\n\nBrak oddanych głosów.\n" + GetNonPollMessage(),
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton
                {
                    Text = "Wycofaj głos spoza ankiety",
                    CallbackData = "unvote"
                }),
                cancellationToken: cancellationToken
            );

            return new SendPollModel
            {
                PollMessage = poll,
                NonPollMessage = nonPoll
            };
        }

        public async Task RemoveVoteFromNonPollAsync(PollChat pollChat, ChatId chatId)
        {
            var messageText = new StringBuilder("<b>Lista głosów poza ankietą:</b>\n\n");
            if (pollChat.NonPollAnswers.Count > 0)
            {
                foreach (var vote in pollChat.NonPollAnswers.OrderBy(np => np.VoteNumber))
                {
                    messageText.AppendLine($"{vote.Username ?? vote.UserFirstName} - {vote.VoteNumber}");
                }
            }
            else
            {
                messageText.AppendLine("Brak oddanych głosów.");
            }
            messageText.AppendLine(GetNonPollMessage());
            try
            {
                await botClientService.BotClient.EditMessageTextAsync(
                    chatId: chatId,
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
            catch (Exception) { }
        }

        public static string GetNonPollMessage()
        {
            return "\n<b>Komendy:</b>\n1. /vote 35000 - pozwala zagłosować poza ankietą\n\n" +
                "<b>Głos się liczy, jeżeli nie zagłosowano w ankiecie.</b>\n<b>Dodawać i cofać głos można co 10 sekund.</b>";
        }
    }
}
