﻿using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.CovidPoll.Exceptions;

namespace Telegram.Bot.CovidPoll.Extensions
{
    public static class DateExtension
    {
        private static Dictionary<PlatformID, string> SystemTime { get; } =
            new Dictionary<PlatformID, string>()
            {
                { PlatformID.Win32NT, "Central European Standard Time" },
                { PlatformID.Unix,  "Europe/Warsaw" }
            };

        //[SupportedOSPlatform("Windows")]
        //[SupportedOSPlatform("Linux")]
        public static DateTimeOffset ConvertUtcToPolishTime(this DateTimeOffset date)
        {
            DateTimeOffset? convertedDate = null;
            if (SystemTime.ContainsKey(Environment.OSVersion.Platform))
            {
                convertedDate = TimeZoneInfo
                    .ConvertTimeBySystemTimeZoneId(date, 
                        SystemTime.FirstOrDefault(s => s.Key == Environment.OSVersion.Platform).Value);
            }
            else
            {
                throw new OSPlatformException();
            }

            return (DateTimeOffset)convertedDate;
        }

        public static DateTimeOffset Midnight(this DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        }
    }
}
