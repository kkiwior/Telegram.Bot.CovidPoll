using System;
using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.xUnit.Services.HostedServices.MemberData
{
    public class CovidTrackingHostedServiceTestsMemberData
    {
        public static IEnumerable<object[]> GetFetchDateWithExpectedResult()
        {
            yield return new object[]
            {
                new DateTimeOffset(2020, 10, 10, 10, 12, 10, new TimeSpan(1, 0, 0)),
                new DateTimeOffset(2020, 10, 11, 10, 12, 10, new TimeSpan(1, 0, 0))
            };

            yield return new object[]
            {
                new DateTimeOffset(2021, 2, 3, 10, 10, 14, new TimeSpan(1, 0, 0)),
                new DateTimeOffset(2021, 2, 4, 10, 10, 14, new TimeSpan(1, 0, 0))
            };

            yield return new object[]
            {
                new DateTimeOffset(2020, 4, 5, 23, 53, 10, new TimeSpan(1, 0, 0)),
                new DateTimeOffset(2020, 4, 6, 23, 53, 10, new TimeSpan(1, 0, 0))
            };
        }
    }
}
