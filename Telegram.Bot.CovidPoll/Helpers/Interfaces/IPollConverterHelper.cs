using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.Helpers.Interfaces
{
    public interface IPollConverterHelper
    {
        List<string> ConvertOptionsToTextOptions(List<int> pollOptions, bool htmlEnabled = false);
    }
}
