using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories;

namespace Telegram.Bot.CovidPoll.Services
{
    public class PollOptionsService
    {
        private readonly ICovidRepository covidRepository;
        private readonly IPollOptionsRepository pollOptionsRepository;

        public PollOptionsService(ICovidRepository covidRepository, IPollOptionsRepository pollOptionsRepository)
        {
            this.covidRepository = covidRepository;
            this.pollOptionsRepository = pollOptionsRepository;
        }

        public async Task<PollOptions> GetPollOptionsAsync(DateTime date)
        {
            var covids = await covidRepository.FindLatestLimitAsync(2);
            if (covids.Count < 2)
                return null;

            var pollOptionsInDb = await pollOptionsRepository.GetByDateAsync(DateTime.UtcNow.Date);
            if (pollOptionsInDb != null)
                return pollOptionsInDb;

            if (covids.FirstOrDefault().Date.Date == date.Date && covids.LastOrDefault().Date.Date == date.Date.AddDays(-1))
            {
                var covidToday = covids.FirstOrDefault().TotalCases - covids.LastOrDefault().TotalCases;
                var pollOptions = new List<string>();
                for (var i = 0; i < 10; i++)
                {
                    var covidOption = 0;
                    if (covidToday / 10000 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 1000 * i : covidToday + 1000 * (i - 4);
                    }
                    else if (covidToday / 1000 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 100 * i : covidToday + 100 * (i - 4);
                    }
                    else if (covidToday / 100 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 10 * i : covidToday + 10 * (i - 4);
                    }
                    else
                    {
                        covidOption = i < 5 ? (covidToday - 1 * i >= 0 ? covidToday - 1 * i : covidToday + 1 * (i - 4)) : covidToday + 1 * (i - 4);
                    }
                    covidOption = covidOption < 0 ? 0 : covidOption;

                    pollOptions.Add(covidOption.ToString());
                }
                pollOptions.Sort();
                var pollOptionsAdd = new PollOptions()
                {
                    Options = pollOptions,
                    Date = DateTime.UtcNow.Date
                };
                await pollOptionsRepository.AddAsync(pollOptionsAdd);
                return pollOptionsAdd;
            }
            return null;
        }
    }
}
