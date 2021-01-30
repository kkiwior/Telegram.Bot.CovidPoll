using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Services.HostedServices;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.xUnit.Services.HostedServices.MemberData;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Services.HostedServices
{
    public class BotPollSenderHostedServiceTests
    {
        #region CONFIG
        private readonly Mock<IOptions<BotSettings>> botSettingsMock;
        private readonly Mock<IBotPollResultSenderService> botPollResultSenderServiceMock;
        private readonly Mock<ITaskDelayProvider> taskDelayProviderMock;
        private readonly Mock<IBotPollSenderService> botPollSenderServiceMock;
        private readonly Mock<IDateProvider> dateProviderMock;
        private readonly BotPollSenderHostedService serviceUnderTests;

        public BotPollSenderHostedServiceTests()
        {
            this.botSettingsMock = new Mock<IOptions<BotSettings>>();
            this.botPollResultSenderServiceMock = new Mock<IBotPollResultSenderService>();
            this.taskDelayProviderMock = new Mock<ITaskDelayProvider>();
            this.botPollSenderServiceMock = new Mock<IBotPollSenderService>();
            this.dateProviderMock = new Mock<IDateProvider>();
            this.serviceUnderTests = new BotPollSenderHostedService(botSettingsMock.Object,
                botPollResultSenderServiceMock.Object, taskDelayProviderMock.Object,
                botPollSenderServiceMock.Object, dateProviderMock.Object);

        }
        #endregion

        #pragma warning disable xUnit1026
        [Theory]
        [MemberData(nameof(BotPollSenderHostedServiceTestsMemberData.GetDatesWithExpectedResults), MemberType = typeof(BotPollSenderHostedServiceTestsMemberData))]
        public async Task WorkerAsync_CurrentDateIsInPollsStartAndPollsEndRangeAndPollsResultIsTrue_ShouldAddOneDayToPollsStart(DateTimeOffset pollsStartDate, DateTimeOffset pollsEndDate, DateTimeOffset expectedPollsStartDate, DateTimeOffset notExpectedPollsEndDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(pollsStartDate);

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsStartProperty = serviceUnderTests.GetType()
                .GetProperty("PollsStart", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsEndProperty = serviceUnderTests.GetType()
                .GetProperty("PollsEnd", BindingFlags.NonPublic | BindingFlags.Instance);

            pollsStartProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsStartDate, pollsStartProperty.PropertyType));

            pollsEndProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsEndDate, pollsEndProperty.PropertyType));

            botPollSenderServiceMock.Setup(b => b.SendPolls(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var calls = 0;
            var stoppingToken = new CancellationTokenSource();
            taskDelayProviderMock.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    calls++;
                    if (calls >= 5)
                        stoppingToken.Cancel();
                });

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            botPollSenderServiceMock.Verify(b => b.SendPolls(It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(expectedPollsStartDate == 
                (DateTimeOffset)pollsStartProperty.GetValue(serviceUnderTests));

            Assert.True(pollsEndDate == (DateTimeOffset)pollsEndProperty.GetValue(serviceUnderTests));

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()));

            botPollResultSenderServiceMock.VerifyNoOtherCalls();
            botPollSenderServiceMock.VerifyNoOtherCalls();
            taskDelayProviderMock.VerifyNoOtherCalls();
        }


        [Theory]
        [MemberData(nameof(BotPollSenderHostedServiceTestsMemberData.GetDatesWithExpectedResults), MemberType = typeof(BotPollSenderHostedServiceTestsMemberData))]
        public async Task WorkerAsync_CurrentDateIsInPollsStartAndPollsEndRangeAndPollsResultIsFalse_ShouldDelayTimeFor5Minutes(DateTimeOffset pollsStartDate, DateTimeOffset pollsEndDate, DateTimeOffset notExpectedPollsStartDate, DateTimeOffset notExpectedPollsEndDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(pollsStartDate);

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsStartProperty = serviceUnderTests.GetType()
                .GetProperty("PollsStart", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsEndProperty = serviceUnderTests.GetType()
                .GetProperty("PollsEnd", BindingFlags.NonPublic | BindingFlags.Instance);

            pollsStartProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsStartDate, pollsStartProperty.PropertyType));

            pollsEndProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsEndDate, pollsEndProperty.PropertyType));

            botPollSenderServiceMock.Setup(b => b.SendPolls(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var calls = 0;
            var stoppingToken = new CancellationTokenSource();
            taskDelayProviderMock.Setup(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    calls++;
                    if (calls >= 3)
                        stoppingToken.Cancel();
                });

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            botPollSenderServiceMock.Verify(b => b.SendPolls(It.IsAny<CancellationToken>()), 
                Times.Exactly(3));

            Assert.True(pollsStartDate == (DateTimeOffset)pollsStartProperty.GetValue(serviceUnderTests));
            Assert.True(pollsEndDate == (DateTimeOffset)pollsEndProperty.GetValue(serviceUnderTests));

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()));
            taskDelayProviderMock.Verify(t => 
                t.Delay(TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Exactly(3));

            botPollResultSenderServiceMock.VerifyNoOtherCalls();
            botPollSenderServiceMock.VerifyNoOtherCalls();
            taskDelayProviderMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(BotPollSenderHostedServiceTestsMemberData.GetDatesWithExpectedResults2), MemberType = typeof(BotPollSenderHostedServiceTestsMemberData))]
        public async Task WorkerAsync_CurrentDateIsGreaterThanPollsEnd_ShouldAddOneDayToPollsEndAndStopPollsAndSendPredictionsToChats(DateTimeOffset dateNow, DateTimeOffset pollsStartDate, DateTimeOffset pollsEndDate, DateTimeOffset expectedPollsStartDate, DateTimeOffset expectedPollsEndDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(dateNow);

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsStartProperty = serviceUnderTests.GetType()
                .GetProperty("PollsStart", BindingFlags.NonPublic | BindingFlags.Instance);

            var pollsEndProperty = serviceUnderTests.GetType()
                .GetProperty("PollsEnd", BindingFlags.NonPublic | BindingFlags.Instance);

            pollsStartProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsStartDate, pollsStartProperty.PropertyType));

            pollsEndProperty.SetValue(serviceUnderTests,
                Convert.ChangeType(pollsEndDate, pollsEndProperty.PropertyType));

            var calls = 0;
            var stoppingToken = new CancellationTokenSource();
            taskDelayProviderMock.Setup(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    calls++;
                    if (calls >= 5)
                        stoppingToken.Cancel();
                });

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            botPollSenderServiceMock.Verify(b => b.StopPolls(It.IsAny<CancellationToken>()), Times.Once);
            botPollResultSenderServiceMock.Verify(b => b.SendPredictionsToChats(), Times.Once);

            Assert.True(expectedPollsStartDate == (DateTimeOffset)pollsStartProperty.GetValue(serviceUnderTests));
            Assert.True(expectedPollsEndDate == 
                (DateTimeOffset)pollsEndProperty.GetValue(serviceUnderTests));

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()));

            botPollResultSenderServiceMock.VerifyNoOtherCalls();
            botPollSenderServiceMock.VerifyNoOtherCalls();
            taskDelayProviderMock.VerifyNoOtherCalls();
        }
    }
}
