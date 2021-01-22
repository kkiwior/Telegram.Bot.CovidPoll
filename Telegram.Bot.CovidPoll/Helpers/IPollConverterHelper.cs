using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public interface IPollConverterHelper
    {
        List<string> ConvertOptionsToTextOptions(List<int> pollOptions, bool htmlEnabled = false);
    }
}
