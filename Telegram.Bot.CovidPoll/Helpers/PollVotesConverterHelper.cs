using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Abstractions;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class PollVotesConverterHelper : IPollVotesConverterHelper
    {
        private readonly IUserRatioRepository userRatioRepository;

        public PollVotesConverterHelper(IUserRatioRepository userRatioRepository)
        {
            this.userRatioRepository = userRatioRepository;
        }

        public IReadOnlyDictionary<int, int> Points { get; } = new Dictionary<int, int>()
        {
            {200, 10},
            {400, 9},
            {600, 8},
            {800, 7},
            {1000, 6},
            {1200, 5},
            {1400, 4},
            {1600, 3},
            {1800, 2},
            {2000, 1}
        };

        public List<PredictionsModel> ConvertPollVotes(PollChat pollChat, int? covidToday = null)
        {
            var pollAnswers = Array.Empty<Answer>()
                .Concat(pollChat.PollAnswers).Concat(pollChat.NonPollAnswers);

            return pollAnswers.Select(pa => new PredictionsModel
            {
                UserId = pa.UserId,
                UserFirstName = pa.UserFirstName,
                Username = pa.Username,
                VoteNumber = pa.VoteNumber,
                Points = covidToday == null ? 0 : GetCovidPoints((int)covidToday, pa.VoteNumber)
            }).ToList();
        }

        public IList<int> GetAllPossibilities(PollChat pollChat)
        {
            var possibilities = new List<int>();

            possibilities.AddRange(pollChat.PollAnswers.Select(pa => pa.VoteNumber));
            possibilities.AddRange(pollChat.NonPollAnswers.Select(npa => npa.VoteNumber));

            return possibilities.Distinct().OrderBy(p => p).ToList();
        }

        public async Task<int?> PredictCovidCasesAsync(Poll poll)
        {
            var pollsChats = poll.ChatPolls.Select(cp => new
            {
                cp.ChatId,
                Answers = Array.Empty<Answer>().Concat(cp.PollAnswers).Concat(cp.NonPollAnswers)
            }).ToList();

            var casesRatio = new List<PredictCovidCasesModel>();

            foreach (var pollChat in pollsChats)
            {
                var usersPoints = await userRatioRepository.GetAsync(pollChat.ChatId);
                foreach (var userVote in pollChat.Answers)
                {
                    var vote = usersPoints.FirstOrDefault(ur => ur.UserId == userVote.UserId);
                    if (vote != null)
                    {
                        casesRatio.Add(new PredictCovidCasesModel
                        {
                            VoteWithoutRatio = userVote.VoteNumber,
                            Vote = userVote.VoteNumber * vote.Ratio,
                            Ratio = vote.Ratio
                        });
                    }
                    else
                    {
                        casesRatio.Add(new PredictCovidCasesModel
                        {
                            VoteWithoutRatio = userVote.VoteNumber,
                            Vote = userVote.VoteNumber,
                            Ratio = 0
                        });
                    }
                }
            }
            if (casesRatio.Count > 0)
            {
                var casesVoteSum = casesRatio.Where(cr => cr.Ratio != 0).Select(cr => cr.Vote).Sum();
                var casesRatioSum = casesRatio.Where(cr => cr.Ratio != 0).Select(cr => cr.Ratio).Sum();
                if (casesRatioSum == 0 || casesVoteSum == 0)
                    return casesRatio.Select(cr => cr.VoteWithoutRatio).Sum() / casesRatio.Count;

                return (int)(casesVoteSum / casesRatioSum);
            }
            return null;
        }

        private int GetCovidPoints(int covidToday, int voteNumber)
        {
            var multiplier = 1;
            if ((double)covidToday / 100000 >= 1)
            {
                multiplier = 3;
            }
            else if ((double)covidToday / 10000 >= 1)
            {
                multiplier = 2;
            }
            if (Math.Abs(covidToday - voteNumber) > Points.Keys.Max())
                return 0;

            return Points
                .FirstOrDefault(p => p.Key * multiplier >= Math.Abs(covidToday - voteNumber)).Value;
        }
    }
}
