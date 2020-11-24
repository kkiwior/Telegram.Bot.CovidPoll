﻿using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.Types;
using Chat = Telegram.Bot.CovidPoll.Db.Chat;
using Poll = Telegram.Bot.CovidPoll.Db.Poll;
using PollAnswer = Telegram.Bot.Types.PollAnswer;

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
            return mongoDb.Polls.Find(p =>
                p.Id == pollId && p.ChatPolls.Any(cp => cp.PollAnswers.Any(pa => pa.UserId == userId))).AnyAsync();
        }
        public Task<bool> CheckIfAlreadyVotedAsync(long userId, ObjectId pollId, string pollChatId)
        {
            return mongoDb.Polls.Find(p => p.Id == pollId && p.ChatPolls.Any(cp =>
                cp.PollId.Equals(pollChatId) && cp.PollAnswers.Any(pa => pa.UserId == userId))).AnyAsync();
        }
        public Task AddVoteAsync(long userId, string username, ObjectId pollId, string pollTelegramId, int vote)
        {
            var pollAnswers = new Db.PollAnswer()
            {
                UserId = userId,
                VoteId = vote,
                Username = username
            };
            return mongoDb.Polls.UpdateOneAsync(p => p.Id == pollId && p.ChatPolls.Any(cp => cp.PollId.Equals(pollTelegramId)),
                Builders<Poll>.Update.Push(p => p.ChatPolls[-1].PollAnswers, pollAnswers));
        }
        public Task RemoveVoteAsync(long userId, ObjectId pollId, string pollTelegramId)
        {
            return mongoDb.Polls.UpdateOneAsync(
                p => p.Id == pollId && p.ChatPolls.Any(cp => cp.PollId.Equals(pollTelegramId)),
                Builders<Poll>.Update.PullFilter(p => p.ChatPolls[-1].PollAnswers, cp => cp.UserId == userId));
        }
    }
}