using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Repositories;

namespace Telegram.Bot.CovidPoll.Services
{
    public class PollOptionsService
    {
        private readonly IPollRepository pollRepository;
        private readonly CovidCalculateService covidCalculateService;

        public PollOptionsService(IPollRepository pollRepository,
                                  CovidCalculateService covidCalculateService)
        {
            this.pollRepository = pollRepository;
            this.covidCalculateService = covidCalculateService;
        }

        public async Task<Poll> GetPollOptionsAsync(DateTime date)
        {
            try
            {
                var cases = await covidCalculateService.GetActualNumberOfCasesAsync();
                if (cases.Date.Date != date.Date.Date)
                    return null;

                var pollOptionsInDb = await pollRepository.GetByDateAsync(date.Date);
                if (pollOptionsInDb != null)
                    return pollOptionsInDb;

                var covidToday = (double) cases.Cases;
                var pollOptions = new List<int>();
                for (var i = 0; i < 10; i++)
                {
                    var covidOption = 0d;
                    if (covidToday / 100000 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 5000 * i : covidToday + 5000 * (i - 4);
                    }
                    else if (covidToday / 10000 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 1000 * i : covidToday + 1000 * (i - 4);
                    }
                    else if (covidToday / 1000 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 500 * i : covidToday + 500 * (i - 4);
                    }
                    else if (covidToday / 100 > 1)
                    {
                        covidOption = i < 5 ? covidToday - 20 * i : covidToday + 20 * (i - 4);
                    }
                    else
                    {
                        if (covidToday < 4)
                            covidOption = i;
                        else
                            covidOption = i < 5 ? (covidToday - 1 * i >= 0 ? covidToday - 1 * i : covidToday + 1 * (i - 4)) : covidToday + 1 * (i - 4);
                    }
                    covidOption = covidOption < 0 ? 0 : covidOption;

                    pollOptions.Add((int) covidOption);
                }

                pollOptions = pollOptions.OrderBy(po => po).ToList();
                var poll = new Poll()
                {
                    Options = pollOptions,
                    Date = DateTime.UtcNow.Date
                };
                await pollRepository.AddAsync(poll);

                return poll;
            }
            catch (CovidCalculateException)
            {
                return null;
            }
        }
    }
}
