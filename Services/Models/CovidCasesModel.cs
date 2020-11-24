using System;

namespace Telegram.Bot.CovidPoll.Services.Models
{
    public class CovidCasesModel
    {
        public DateTime Date { get; set; }
        public int Cases { get; set; }
    }
}
