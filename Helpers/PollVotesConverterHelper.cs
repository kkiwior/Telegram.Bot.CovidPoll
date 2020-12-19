using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Services.Models;

namespace Telegram.Bot.CovidPoll.Helpers
{
    public class PollVotesConverterHelper
    {
        private readonly IReadOnlyDictionary<int, int> points = new Dictionary<int, int>()
        {
            {100, 10},
            {300, 9},
            {500, 8},
            {700, 7},
            {900, 6},
            {1100, 5},
            {1500, 4},
            {1900, 3},
            {2400, 2},
            {3000, 1}
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
                Points = covidToday != null ? points.FirstOrDefault(p => p.Key >= Math.Abs((int) covidToday - poll.Options[pa.VoteId])).Value : 0,
                FromPoll = true
            }).ToList());
            answers.AddRange(pollChat.NonPollAnswers.Select(pa => new PredictionsModel
            {
                UserId = pa.UserId,
                UserFirstName = pa.UserFirstName,
                Username = pa.Username,
                VoteNumber = pa.VoteNumber,
                Points = covidToday != null ? points.FirstOrDefault(p => p.Key >= Math.Abs((int) covidToday - pa.VoteNumber)).Value : 0,
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
    }
}
