using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class PollConverterHelper : IPollConverterHelper
    {
        public List<string> ConvertOptionsToTextOptions(List<int> pollOptions, bool htmlEnabled = false)
        {
            return pollOptions.Select(po => po.ToString("### ###")).ToList();

            //if (!htmlEnabled)
            //    return pollOptions
            //        .Select((o, index) => index == 0 ? $"<{o:### ###}" : $"{o:### ###}")
            //        .Select((o, index) => index == pollOptions.Count - 1 ? $">{o:### ###}" : $"{o:### ###}")
            //        .ToList();

            //return pollOptions
            //    .Select((o, index) => index == 0 ? $"&lt;{o:### ###}" : $"{o:### ###}")
            //    .Select((o, index) => index == pollOptions.Count - 1 ? $"&gt;{o:### ###}" : $"{o:### ###}")
            //    .ToList();
        }
    }
}
