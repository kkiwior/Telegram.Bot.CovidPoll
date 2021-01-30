using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.xUnit.Integration.Fixtures;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Integration.Repositories
{
    public class PollChatRankingRepositoryTests : IClassFixture<MongoFixture>
    {
        #region CONFIG
        private readonly MongoDb dbContext;
        private readonly PollChatRankingRepository repositoryUnderTests;

        public PollChatRankingRepositoryTests(MongoFixture mongoFixture)
        {
            this.dbContext = mongoFixture.MongoDbContext;

            var userRatioRepository = new UserRatioRepository(dbContext);
            this.repositoryUnderTests = new PollChatRankingRepository(dbContext, userRatioRepository,
                new PollVotesConverterHelper(userRatioRepository));
        }
        #endregion

        [Fact]
        public async Task AddWinsCountAsync_RankingAndUserDontExistInDb_ShouldAddRankingAndUserWithComputedRatio()
        {
            //Arrange
            await dbContext.ChatsRankings.DeleteManyAsync(Builders<ChatRanking>.Filter.Empty);
            await dbContext.UsersRatio.DeleteManyAsync(Builders<UserRatio>.Filter.Empty);

            var chatId = 100L;
            var winners = new List<PredictionsModel>()
            {
                new PredictionsModel()
                {
                    UserId = 1,
                    Points = 5,
                    UserFirstName = "test",
                    Username = "test2",
                    VoteNumber = 220
                }
            };
            var chatWinnerExpected = new ChatWinner()
            {
                UserId = 1,
                Points = 5,
                UserFirstName = "test",
                Username = "test2",
                TotalVotes = 1,
                WinsCount = 1
            };
            var userExpected = new UserRatio()
            {
                ChatId = chatId,
                Ratio = 0.5,
                UserId = 1
            };

            //Act
            await repositoryUnderTests.AddWinsCountAsync(winners, chatId);

            //Assert
            var chatRanking = await dbContext.ChatsRankings.Find(c => c.ChatId == chatId)
                .FirstOrDefaultAsync();

            var chatWinner = chatRanking.Winners.FirstOrDefault(w => w.UserId == 1);

            var userRatio = await dbContext.UsersRatio.Find(u => u.UserId == userExpected.UserId)
                .FirstOrDefaultAsync();

            userExpected.Id = userRatio == null ? new ObjectId() : userRatio.Id;

            chatWinnerExpected.Should().BeEquivalentTo(chatWinner);
            userExpected.Should().BeEquivalentTo(userRatio);
        }

        [Fact]
        public async Task AddWinsCountAsync_RankingAndUserExistInDb_ShouldModifyRankingAndUserWithComputedRatio()
        {
            //Arrange
            await dbContext.ChatsRankings.DeleteManyAsync(Builders<ChatRanking>.Filter.Empty);
            await dbContext.UsersRatio.DeleteManyAsync(Builders<UserRatio>.Filter.Empty);

            await dbContext.UsersRatio.InsertOneAsync(new UserRatio()
            {
                UserId = 1,
                Ratio = 0.5,
                ChatId = 100
            });

            await dbContext.ChatsRankings.InsertOneAsync(new ChatRanking()
            {
                ChatId = 100,
                Winners = new List<ChatWinner>()
                {
                    new ChatWinner()
                    {
                        UserId = 1,
                        Points = 5,
                        UserFirstName = "test",
                        Username = "test2",
                        TotalVotes = 1,
                        WinsCount = 1
                    }
                }
            });

            var chatId = 100L;
            var winners = new List<PredictionsModel>()
            {
                new PredictionsModel()
                {
                    UserId = 1,
                    Points = 10,
                    UserFirstName = "test11",
                    Username = "test22",
                    VoteNumber = 400
                }
            };
            var chatWinnerExpected = new ChatWinner()
            {
                UserId = 1,
                Points = 15,
                UserFirstName = "test11",
                Username = "test22",
                TotalVotes = 2,
                WinsCount = 2
            };
            var userExpected = new UserRatio()
            {
                ChatId = chatId,
                Ratio = 0.75,
                UserId = 1
            };

            //Act
            await repositoryUnderTests.AddWinsCountAsync(winners, chatId);

            //Assert
            var chatRanking = await dbContext.ChatsRankings.Find(c => c.ChatId == chatId)
                .FirstOrDefaultAsync();

            var chatWinner = chatRanking.Winners.FirstOrDefault(w => w.UserId == 1);

            var userRatio = await dbContext.UsersRatio.Find(u => u.UserId == userExpected.UserId)
                .FirstOrDefaultAsync();

            userExpected.Id = userRatio == null ? new ObjectId() : userRatio.Id;

            chatWinnerExpected.Should().BeEquivalentTo(chatWinner);
            userExpected.Should().BeEquivalentTo(userRatio);
        }
    }
}
