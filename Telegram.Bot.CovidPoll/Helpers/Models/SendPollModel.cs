using Telegram.Bot.Types;

namespace Telegram.Bot.CovidPoll.Helpers.Models
{
    public class SendPollModel
    {
        public Message PollMessage { get; set; }
        public Message NonPollMessage { get; set; }
    }
}
