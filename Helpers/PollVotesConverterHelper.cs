using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services.Models;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class PollVotesConverterHelper
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
        public IList<PredictionsModel> ConvertPollVotes(Poll poll, PollChat pollChat, int? covidToday = null)
        {
            var answers = new List<PredictionsModel>();

            answers.AddRange(pollChat.PollAnswers.Select(pa => new PredictionsModel
            {
                UserId = pa.UserId,
                UserFirstName = pa.UserFirstName,
                Username = pa.Username,
                VoteNumber = poll.Options[pa.VoteId],
                Points = covidToday != null ? GetCovidPoints((int) covidToday, poll.Options[pa.VoteId]) : 0,
                FromPoll = true
            }).ToList());
            answers.AddRange(pollChat.NonPollAnswers.Select(pa => new PredictionsModel
            {
                UserId = pa.UserId,
                UserFirstName = pa.UserFirstName,
                Username = pa.Username,
                VoteNumber = pa.VoteNumber,
                Points = covidToday != null ? GetCovidPoints((int) covidToday, pa.VoteNumber) : 0,
                FromPoll = false
            }).ToList());

            return answers;
        }

        public IList<PossibilitiesModel> GetAllPossibilities(Poll poll, PollChat pollChat)
        {
            var possibilities = new List<PossibilitiesModel>();

            pollChat.PollAnswers.ForEach(pa =>
            {
                int pollVote = -1;
                if (pa.VoteId != 0 && pa.VoteId != 9)
                    pollVote = poll.Options[pa.VoteId];

                if (possibilities.FirstOrDefault(p => p.VoteNumber == poll.Options[pa.VoteId]) == null)
                    possibilities.Add(new PossibilitiesModel
                    {
                        VoteNumber = poll.Options[pa.VoteId],
                        FromPoll = true
                    });

                if (pollVote == -1)
                    return;

                pollChat.NonPollAnswers.RemoveAll(npa => npa.VoteNumber == pollVote);
            });
            pollChat.NonPollAnswers.ForEach(npa =>
            {
                possibilities.Add(new PossibilitiesModel
                {
                    VoteNumber = npa.VoteNumber,
                    FromPoll = false
                });
            });
            possibilities = possibilities.OrderBy(p => p.VoteNumber).ThenByDescending(p => p.FromPoll).ToList();

            return possibilities;
        }

        public async Task<int> PredictCovidCasesAsync(Poll poll)
        {
            var pollsChats = poll.ChatPolls.Select(cp => new 
            { 
                cp.ChatId,
                cp.PollAnswers, 
                cp.NonPollAnswers 
            }).ToList();
            var casesRatio = new List<PredictCovidCasesModel>();
            foreach (var pollChat in pollsChats)
            {
                var usersPoints = await userRatioRepository.GetAsync(pollChat.ChatId);
                foreach (var userVote in pollChat.PollAnswers)
                {
                    var vote = usersPoints.FirstOrDefault(ur => ur.UserId == userVote.UserId);
                    if (vote != null)
                    {
                        casesRatio.Add(new PredictCovidCasesModel
                        {
                            VoteWithoutRatio = poll.Options[userVote.VoteId],
                            Vote = poll.Options[userVote.VoteId] * vote.Ratio,
                            Ratio = vote.Ratio
                        });
                    }
                    else
                    {
                        casesRatio.Add(new PredictCovidCasesModel
                        {
                            VoteWithoutRatio = poll.Options[userVote.VoteId],
                            Vote = poll.Options[userVote.VoteId] * 0.9,
                            Ratio = 0.9
                        });
                    }
                }
                foreach (var userVote in pollChat.NonPollAnswers)
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
                            Vote = userVote.VoteNumber * 0.9,
                            Ratio = 0.9
                        });
                    }
                }
            }
            if (casesRatio.Count > 0)
            {
                var casesVoteSum = casesRatio.Select(cr => cr.Vote).Sum();
                var casesRatioSum = casesRatio.Select(cr => cr.Ratio).Sum();
                if (casesRatioSum == 0 || casesVoteSum == 0)
                    return casesRatio.Select(cr => cr.VoteWithoutRatio).Sum() / casesRatio.Count;

                return (int) (casesVoteSum / casesRatioSum);
            }

            throw new PredictCovidCasesException();
        }

        private int GetCovidPoints(int covidToday, int voteNumber)
        {
            var multiplier = 1;
            if ((double) covidToday / 100000 >= 1)
            {
                multiplier = 3;
            }
            else if((double) covidToday / 10000 >= 1)
            {
                multiplier = 2;
            }
            return Points.FirstOrDefault(p => p.Key * multiplier >= Math.Abs(covidToday - voteNumber)).Value;
        }
    }
}
