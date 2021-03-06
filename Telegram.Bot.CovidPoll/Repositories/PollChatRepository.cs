﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;

namespace Telegram.Bot.CovidPoll.Repositories
{
    public class PollChatRepository : IPollChatRepository
    {
        private readonly MongoDb mongoDb;
        private readonly IPollRepository pollRepository; 

        public PollChatRepository(MongoDb mongoDb, IPollRepository pollRepository)
        {
            this.mongoDb = mongoDb;
            this.pollRepository = pollRepository;
        }

        public Task AddAsync(ObjectId pollId, PollChat pollChat)
        {
            return mongoDb.Polls.UpdateOneAsync(p => p.Id == pollId,
                Builders<Poll>.Update.Push(pl => pl.ChatPolls, pollChat));
        }

        public Task<bool> CheckIfAlreadyVotedInAllAsync(long userId, ObjectId pollId)
        {
            return mongoDb.Polls
                .Find(p => p.Id == pollId && p.ChatPolls
                    .Any(cp => cp.PollAnswers.Any(pa => pa.UserId == userId)))
                .AnyAsync();
        }

        public Task<bool> CheckIfAlreadyVotedAsync(long userId, ObjectId pollId, string pollChatId)
        {
            return mongoDb.Polls.Find(p => p.Id == pollId && p.ChatPolls.Any(cp =>
                cp.PollId.Equals(pollChatId) && cp.PollAnswers.Any(pa => pa.UserId == userId))).AnyAsync();
        }

        public Task<bool> CheckIfAlreadyVotedInNonPollAsync(long userId, ObjectId pollId, 
            string pollChatId)
        {
            return mongoDb.Polls.Find(p => p.Id == pollId && p.ChatPolls.Any(cp =>
                    cp.PollId.Equals(pollChatId) && cp.NonPollAnswers.Any(pa => pa.UserId == userId)))
                .AnyAsync();
        }

        public async Task<bool> CheckIfAlreadyVotedInPollOrNonPollAsync(long userId, ObjectId pollId, 
            string pollChatId)
        {
            var poll = await mongoDb.Polls.Find(p => p.Id == pollId && p.ChatPolls.Any(cp =>
                    cp.PollId.Equals(pollChatId) && cp.PollAnswers.Any(pa => pa.UserId == userId)))
                .AnyAsync();

            var nonPoll = await mongoDb.Polls.Find(p => p.Id == pollId && p.ChatPolls.Any(cp =>
                    cp.PollId.Equals(pollChatId) && cp.NonPollAnswers.Any(np => np.UserId == userId)))
                .AnyAsync();

            return poll | nonPoll;
        }

        public Task AddVoteAsync(long userId, string username, string userFirstName, 
            ObjectId pollId, string pollTelegramId, int vote)
        {
            var pollAnswers = new Db.PollAnswer()
            {
                UserId = userId,
                VoteNumber = vote,
                Username = username,
                UserFirstName = userFirstName
            };
            return mongoDb.Polls.UpdateOneAsync(p => p.Id == pollId && p.ChatPolls
                .Any(cp => cp.PollId.Equals(pollTelegramId)), 
                Builders<Poll>.Update.Push(p => p.ChatPolls[-1].PollAnswers, pollAnswers));
        }

        public Task RemoveVoteAsync(long userId, ObjectId pollId, string pollTelegramId)
        {
            return mongoDb.Polls.UpdateOneAsync(
                p => p.Id == pollId && p.ChatPolls.Any(cp => cp.PollId.Equals(pollTelegramId)),
                Builders<Poll>.Update
                .PullFilter(p => p.ChatPolls[-1].PollAnswers, cp => cp.UserId == userId));
        }

        public async Task<PollChat> FindLatestByChatIdAsync(long chatId)
        {
            var poll = await pollRepository.FindLatestAsync();
            return poll.ChatPolls.Where(cp => cp.ChatId == chatId).FirstOrDefault();
        }

        public async Task UpdateLastCommandDateAsync(long chatId, DateTime date)
        {
            var poll = await pollRepository.FindLatestWithoutChatsAsync();

            await mongoDb.Polls
                .UpdateOneAsync(p => p.Id == poll.Id && p.ChatPolls.Any(cp => cp.ChatId == chatId), 
                    Builders<Poll>.Update.Set(p => p.ChatPolls[-1].LastCommandDate, date));
        }

        public Task AddNonPollVoteAsync(long userId, string username, string userFirstName, 
            ObjectId pollId, string pollTelegramId, int voteNumber)
        {
            var nonPollAnswers = new Db.NonPollAnswer()
            {
                UserId = userId,
                Username = username,
                UserFirstName = userFirstName,
                VoteNumber = voteNumber
            };
            return mongoDb.Polls.UpdateOneAsync(p => p.Id == pollId && p.ChatPolls
                .Any(cp => cp.PollId.Equals(pollTelegramId)),
                Builders<Poll>.Update.Push(p => p.ChatPolls[-1].NonPollAnswers, nonPollAnswers));
        }

        public Task RemoveNonPollVoteAsync(long userId, ObjectId pollId, string pollTelegramId)
        {
            return mongoDb.Polls.UpdateOneAsync(
                p => p.Id == pollId && p.ChatPolls.Any(cp => cp.PollId.Equals(pollTelegramId)),
                Builders<Poll>.Update
                .PullFilter(p => p.ChatPolls[-1].NonPollAnswers, cp => cp.UserId == userId));
        }
    }
}
