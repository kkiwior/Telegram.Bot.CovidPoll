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

namespace Telegram.Bot.CovidPoll.Handlers
{
    public class BotRankingHandler : IBotEvent
    {
        private readonly BotClientService botClientService;
        private readonly IBotCommandHelper botCommandHelper;
        private readonly IPollChatRankingRepository pollChatRankingRepository;
        public BotRankingHandler(BotClientService botClientService,
                                 IBotCommandHelper botCommandHelper,
                                 IPollChatRankingRepository pollChatRankingRepository)
        {
            this.botClientService = botClientService;
            this.botCommandHelper = botCommandHelper;
            this.pollChatRankingRepository = pollChatRankingRepository;
        }
        public IList<BotCommand> Command => 
            new List<BotCommand> 
            { 
                new BotCommand 
                { 
                    Command = "ranking", 
                    Description = "Pokazuje aktualny ranking osób najlepiej przewidujących (nie wyświetli nic, jeżeli nie było jeszcze wyników przewidywań)"
                } 
            };

        public void RegisterEvent(BotClientService botClient)
        {
            botClientService.BotClient.OnMessage += BotClient_OnMessage;
        }

        private async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            var n = await botCommandHelper.CheckCommandIsCorrectAsync(BotCommandHelper.BotCommands.ranking, e.Message.Text);
            if (n.CommandCorrect && e.Message.Chat.Type == ChatType.Supergroup || e.Message.Chat.Type == ChatType.Group)
            {
                var ranking = await pollChatRankingRepository.GetChatRankingAsync(e.Message.Chat.Id);
                if (ranking == null || ranking.LastCommandDate.AddMinutes(5) >= DateTime.UtcNow)
                    return;

                await pollChatRankingRepository.UpdateLastCommandDateAsync(e.Message.Chat.Id, DateTime.UtcNow);
                var sb = new StringBuilder("<b>Ogólny ranking (przewidzieli poprawnie):</b>"); sb.AppendLine();
                if (ranking.Winners.Count == 0)
                {
                    sb.AppendLine(); sb.AppendLine("Brak osób, które poprawnie przewidziały.");
                }

                foreach (var winner in ranking.Winners.OrderByDescending(w => w.WinsCount).Select((value, index) => new { value, index }))
                {
                    sb.AppendLine($"{winner.index + 1}. {winner.value.Username ?? winner.value.UserFirstName} - {winner.value.WinsCount} raz/y");
                }
                sb.AppendLine(); sb.AppendLine("Komendę można wywołać co 5 minut.");
                try
                {
                    await botClientService.BotClient.SendTextMessageAsync(
                        chatId: e.Message.Chat.Id,
                        text: sb.ToString(),
                        parseMode: ParseMode.Html
                    );
                }
                catch (Exception) {}
            }
        }
    }
}
