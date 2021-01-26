using System;
using System.Collections.Generic;

namespace Telegram.Bot.CovidPoll.xUnit.Services.HostedServices.MemberData
{
    public class BotPollSenderHostedServiceTestsMemberData
    {
        public static IEnumerable<object[]> GetDatesWithExpectedResults()
        {
            yield return new object[]
            {
                new DateTimeOffset(2020, 10, 10, 10, 12, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 10, 10, 12, 12, 10, TimeSpan.Zero),

                new DateTimeOffset(2020, 10, 11, 10, 12, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 10, 11, 12, 12, 10, TimeSpan.Zero)
            };

            yield return new object[]
            {
                new DateTimeOffset(2021, 2, 3, 10, 10, 14, TimeSpan.Zero),
                new DateTimeOffset(2021, 2, 3, 13, 10, 14, TimeSpan.Zero),


                new DateTimeOffset(2021, 2, 4, 10, 10, 14, TimeSpan.Zero),
                new DateTimeOffset(2021, 2, 4, 13, 10, 14, TimeSpan.Zero)
            };

            yield return new object[]
            {
                new DateTimeOffset(2020, 4, 5, 22, 53, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 4, 5, 23, 53, 10, TimeSpan.Zero),


                new DateTimeOffset(2020, 4, 6, 22, 53, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 4, 6, 23, 53, 10, TimeSpan.Zero)
            };
        }

        public static IEnumerable<object[]> GetDatesWithExpectedResults2()
        {
            yield return new object[]
            {
                new DateTimeOffset(2020, 10, 10, 12, 12, 10, TimeSpan.Zero),

                new DateTimeOffset(2020, 11, 10, 10, 12, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 10, 10, 12, 12, 10, TimeSpan.Zero),

                new DateTimeOffset(2020, 11, 10, 10, 12, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 10, 11, 12, 12, 10, TimeSpan.Zero)
            };

            yield return new object[]
            {
                new DateTimeOffset(2021, 2, 3, 13, 10, 14, TimeSpan.Zero),

                new DateTimeOffset(2021, 2, 4, 10, 10, 14, TimeSpan.Zero),
                new DateTimeOffset(2021, 2, 3, 13, 10, 14, TimeSpan.Zero),


                new DateTimeOffset(2021, 2, 4, 10, 10, 14, TimeSpan.Zero),
                new DateTimeOffset(2021, 2, 4, 13, 10, 14, TimeSpan.Zero)
            };

            yield return new object[]
            {
                new DateTimeOffset(2020, 4, 5, 23, 53, 10, TimeSpan.Zero),

                new DateTimeOffset(2020, 4, 6, 22, 53, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 4, 5, 23, 53, 10, TimeSpan.Zero),

                new DateTimeOffset(2020, 4, 6, 22, 53, 10, TimeSpan.Zero),
                new DateTimeOffset(2020, 4, 6, 23, 53, 10, TimeSpan.Zero)
            };
        }
    }
}
