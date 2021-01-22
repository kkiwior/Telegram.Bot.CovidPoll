using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Extensions
{
    public static class Extensions
    {
        public static async Task UnpinChatMessageByIdAsync(this ITelegramBotClient telegramBotClient,
                                                           ChatId chatId,
                                                           int messageId,
                                                           CancellationToken cancellationToken = default)
        {
            await telegramBotClient.MakeRequestAsync(new UnpinChatMessageByIdRequest(chatId, messageId), cancellationToken);
        }
    }

    [JsonObject(MemberSerialization.OptIn, NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class UnpinChatMessageByIdRequest : RequestBase<bool>
    {
        [JsonProperty(Required = Required.Always)]
        public ChatId ChatId { get; }

        [JsonProperty(Required = Required.Always)]
        public int MessageId { get; }

        public UnpinChatMessageByIdRequest(ChatId chatId, int messageId) : base("unpinChatMessage")
        {
            ChatId = chatId;
            MessageId = messageId;
        }
    }
}
