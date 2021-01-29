using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.xUnit.Integration.Fixtures;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Integration.Repositories
{
    public class PollChatRepositoryTests : IClassFixture<MongoFixture>
    {
        #region CONFIG
        private readonly MongoDb dbContext;
        private readonly PollChatRepository repositoryUnderTests;
        private readonly Mock<IPollRepository> pollRepositoryMock;

        public PollChatRepositoryTests(MongoFixture mongoFixture)
        {
            this.dbContext = mongoFixture.MongoDbContext;
            this.pollRepositoryMock = new Mock<IPollRepository>();
            this.repositoryUnderTests = new PollChatRepository(mongoFixture.MongoDbContext, 
                pollRepositoryMock.Object);
        }
        #endregion

        [Fact]
        public async Task CheckIfAlreadyVotedAsync_VoteIsInDb_ShouldReturnTrue()
        {
            //Arrange
            await dbContext.Polls.DeleteManyAsync(Builders<Poll>.Filter.Empty);

            var pollId = ObjectId.GenerateNewId();
            var pollChatId = "2";
            var userId = 5;

            var poll = new Poll()
            {
                Id = pollId,
                ChatPolls = new List<PollChat>()
                {
                    new PollChat()
                    {
                        PollId = pollChatId,
                        PollAnswers = new List<PollAnswer>()
                        {
                            new PollAnswer()
                            {
                                UserId = userId
                            }
                        }
                    }
                }
            };

            await dbContext.Polls.InsertOneAsync(poll);

            //Act
            var result = await repositoryUnderTests.CheckIfAlreadyVotedAsync(userId, pollId, pollChatId);

            //Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckIfAlreadyVotedAsync_VoteIsNotDb_ShouldReturnFalse()
        {
            //Arrange
            await dbContext.Polls.DeleteManyAsync(Builders<Poll>.Filter.Empty);

            var pollId = ObjectId.GenerateNewId();
            var pollChatId = "2";
            var userId = 5;

            //Act
            var result = await repositoryUnderTests.CheckIfAlreadyVotedAsync(userId, pollId, pollChatId);

            //Assert
            Assert.False(result);
        }
    }
}
