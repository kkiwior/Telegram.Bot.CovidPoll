using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Helpers;
using Telegram.Bot.CovidPoll.Helpers.Models;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.xUnit.Helpers.MemberData;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Helpers
{
    public class PollVotesConverterHelperTests
    {
        #region CONFIG
        private readonly Mock<IUserRatioRepository> userRatioRepositoryMock;
        private readonly PollVotesConverterHelper helperUnderTests;

        public PollVotesConverterHelperTests()
        {
            this.userRatioRepositoryMock = new Mock<IUserRatioRepository>();
            this.helperUnderTests = new PollVotesConverterHelper(userRatioRepositoryMock.Object);
        }
        #endregion

        [Theory]
        [MemberData(nameof(PollVotesConverterHelperTestsMemberData.GetAnswersWithExpectedResults), MemberType = typeof(PollVotesConverterHelperTestsMemberData))]
        public void ConvertPollVotes_ShouldReturnCorrectResults(PollChat pollChat, List<PredictionsModel> expectedResult, int? covidToday)
        {
            //Arrange

            //Act
            var result = helperUnderTests.ConvertPollVotes(pollChat, covidToday);

            //Assert
            expectedResult.Should().BeEquivalentTo(result);
        }

        [Theory]
        [MemberData(nameof(PollVotesConverterHelperTestsMemberData.GetPollWithExpectedResults), MemberType = typeof(PollVotesConverterHelperTestsMemberData))]
        public async Task PredictCovidCasesAsync_ShouldReturnCorrectResults(Poll poll, List<UserRatio> usersRatios, int? expectedResult)
        {
            //Arrange
            userRatioRepositoryMock.Setup(u => u.GetAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => usersRatios.Where(u => u.ChatId == id).ToList());

            //Act
            var result = await helperUnderTests.PredictCovidCasesAsync(poll);

            //Assert
            Assert.Equal(result, expectedResult);
        }
    }
}
