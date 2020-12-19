using System.Collections.Generic;
using System.Linq;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class PollConverterHelper : IPollConverterHelper
    {
        public List<string> ConvertOptionsToTextOptions(List<int> pollOptions, bool htmlEnabled = false)
        {
            var pollOptionsString = new List<string>();
            if (!htmlEnabled)
                return pollOptions
                    .Select((o, index) => index == 0 ? $"<{o:### ###}" : $"{o:### ###}")
                    .Select((o, index) => index == pollOptions.Count - 1 ? $">{o:### ###}" : $"{o:### ###}")
                    .ToList();

            return pollOptions
                .Select((o, index) => index == 0 ? $"&lt;{o:### ###}" : $"{o:### ###}")
                .Select((o, index) => index == pollOptions.Count - 1 ? $"&gt;{o:### ###}" : $"{o:### ###}")
                .ToList();
        }
    }
}
