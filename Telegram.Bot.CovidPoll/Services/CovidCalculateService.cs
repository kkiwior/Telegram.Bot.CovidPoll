using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.Services.Models;

namespace Telegram.Bot.CovidPoll.Services
{
    public class CovidCalculateService : ICovidCalculateService
    {
        private readonly ICovidRepository covidRepository;

        public CovidCalculateService(ICovidRepository covidRepository)
        {
            this.covidRepository = covidRepository;
        }

        public async Task<CovidCasesModel> GetActualNumberOfCasesAsync()
        {
            //var covids = await covidRepository.FindLatestLimitAsync(2);
            //if (covids.Count < 2)
            //    throw new CovidCalculateException();

            //if (covids.FirstOrDefault().Date.Date == covids.LastOrDefault().Date.Date.AddDays(1))
            //{
            //    return new CovidCasesModel()
            //    {
            //        Date = covids.FirstOrDefault().Date.Date,
            //        Cases = Math.Abs(covids.FirstOrDefault().TotalCases - covids.LastOrDefault().TotalCases)
            //    };
            //}
            //throw new CovidCalculateException();

            var covids = await covidRepository.FindLatestAsync();
            if (covids == null)
                throw new CovidCalculateException();

            return new CovidCasesModel()
            {
                Date = covids.Date.Date,
                Cases = covids.NewCases
            };
        }
    }
}
