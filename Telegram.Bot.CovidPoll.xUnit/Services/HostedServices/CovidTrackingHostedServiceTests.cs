using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Services.HostedServices;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.xUnit.Fixtures;
using Telegram.Bot.CovidPoll.xUnit.Services.HostedServices.MemberData;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Services.HostedServices
{
    public class CovidTrackingHostedServiceTests : IClassFixture<AppSettingsFixture>
    {
        #region CONFIG
        private readonly Mock<IOptions<CovidTrackingSettings>> covidTrackingSettingsMock;
        private readonly Mock<IHostApplicationLifetime> applicationLifetimeMock;
        private readonly Mock<ILogger<CovidTrackingHostedService>> logMock;
        private readonly Mock<ICovidDownloadingService> covidDownloadingServiceMock;
        private readonly Mock<ITaskDelayProvider> taskDelayProviderMock;
        private readonly CovidTrackingHostedService serviceUnderTests;
        private readonly Mock<IDateProvider> dateProviderMock;

        public CovidTrackingHostedServiceTests(AppSettingsFixture appSettingsFixture)
        {
            this.covidTrackingSettingsMock = new Mock<IOptions<CovidTrackingSettings>>();
            this.applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            this.logMock = new Mock<ILogger<CovidTrackingHostedService>>();
            this.covidDownloadingServiceMock = new Mock<ICovidDownloadingService>();
            this.taskDelayProviderMock = new Mock<ITaskDelayProvider>();
            this.dateProviderMock = new Mock<IDateProvider>();
            this.serviceUnderTests = new CovidTrackingHostedService(covidTrackingSettingsMock.Object,
                applicationLifetimeMock.Object, logMock.Object, covidDownloadingServiceMock.Object,
                taskDelayProviderMock.Object, dateProviderMock.Object);
        }
        #endregion

        [Theory]
        [MemberData(nameof(CovidTrackingHostedServiceTestsMemberData.GetFetchDateWithExpectedResult), 
            MemberType = typeof(CovidTrackingHostedServiceTestsMemberData))]
        public async Task ExecuteAsync_DownloadCovidByJsonAsyncReturnsTrue_ShouldSetDateAsExpected(DateTimeOffset fetchDate, DateTimeOffset expectedDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(fetchDate);

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var property = serviceUnderTests.GetType()
                .GetProperty("FetchDate", BindingFlags.NonPublic | BindingFlags.Instance);

            property.SetValue(serviceUnderTests,
                Convert.ChangeType(fetchDate, property.PropertyType));

            covidDownloadingServiceMock.Setup(c => c.DownloadCovidByJsonAsync()).ReturnsAsync(true);

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
                method.Invoke(serviceUnderTests, new[] { (object) stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            covidDownloadingServiceMock.Verify(c => c.DownloadCovidByJsonAsync(), Times.Once);
            Assert.True(expectedDate == (DateTimeOffset)property.GetValue(serviceUnderTests));

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()));
            
            taskDelayProviderMock.VerifyNoOtherCalls();
            applicationLifetimeMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(CovidTrackingHostedServiceTestsMemberData.GetFetchDateWithExpectedResult),
            MemberType = typeof(CovidTrackingHostedServiceTestsMemberData))]
        public async Task ExecuteAsync_DownloadCovidByJsonAsyncReturnsFalseFetchDelayIs2Hours_ShouldDelayFor20Minutes(DateTimeOffset fetchDate, DateTimeOffset expectedDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(fetchDate.AddHours(2));

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var property = serviceUnderTests.GetType()
                .GetProperty("FetchDate", BindingFlags.NonPublic | BindingFlags.Instance);

            property.SetValue(serviceUnderTests,
                Convert.ChangeType(fetchDate, property.PropertyType));

            covidDownloadingServiceMock.Setup(c => c.DownloadCovidByJsonAsync()).ReturnsAsync(false);

            var stoppingToken = new CancellationTokenSource();
            taskDelayProviderMock.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback(() =>{ stoppingToken.Cancel(); });

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            Assert.True(fetchDate == (DateTimeOffset)property.GetValue(serviceUnderTests));

            covidDownloadingServiceMock.Verify(c => c.DownloadCovidByJsonAsync(), Times.Once);
            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromMinutes(20), 
                It.IsAny<CancellationToken>()), Times.Once);

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1),
                It.IsAny<CancellationToken>()));

            taskDelayProviderMock.VerifyNoOtherCalls();
            applicationLifetimeMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(CovidTrackingHostedServiceTestsMemberData.GetFetchDateWithExpectedResult),
            MemberType = typeof(CovidTrackingHostedServiceTestsMemberData))]
        public async Task ExecuteAsync_DownloadCovidByJsonAsyncReturnsFalseFetchDelayIs3Hours_ShouldDelayFor3Hours(DateTimeOffset fetchDate, DateTimeOffset expectedDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(fetchDate.AddHours(3));

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var property = serviceUnderTests.GetType()
                .GetProperty("FetchDate", BindingFlags.NonPublic | BindingFlags.Instance);

            property.SetValue(serviceUnderTests,
                Convert.ChangeType(fetchDate, property.PropertyType));

            covidDownloadingServiceMock.Setup(c => c.DownloadCovidByJsonAsync()).ReturnsAsync(false);

            var stoppingToken = new CancellationTokenSource();
            taskDelayProviderMock.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback(() => { stoppingToken.Cancel(); });

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            Assert.True(fetchDate == (DateTimeOffset)property.GetValue(serviceUnderTests));

            covidDownloadingServiceMock.Verify(c => c.DownloadCovidByJsonAsync(), Times.Once);
            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromHours(3),
                It.IsAny<CancellationToken>()), Times.Once);

            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1),
                It.IsAny<CancellationToken>()));

            taskDelayProviderMock.VerifyNoOtherCalls();
            applicationLifetimeMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(CovidTrackingHostedServiceTestsMemberData.GetFetchDateWithExpectedResult),
            MemberType = typeof(CovidTrackingHostedServiceTestsMemberData))]
        public async Task ExecuteAsync_DownloadCovidByJsonAsyncThrowsCovidParseException_ShouldStopApplication(DateTimeOffset fetchDate, DateTimeOffset expectedDate)
        {
            //Arrange
            dateProviderMock.Setup(d => d.DateTimeOffsetUtcNow()).Returns(fetchDate);

            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var property = serviceUnderTests.GetType()
                .GetProperty("FetchDate", BindingFlags.NonPublic | BindingFlags.Instance);

            property.SetValue(serviceUnderTests,
                Convert.ChangeType(fetchDate, property.PropertyType));

            covidDownloadingServiceMock.Setup(c => c.DownloadCovidByJsonAsync())
                .Throws(new CovidParseException());

            var stoppingToken = new CancellationTokenSource();
            applicationLifetimeMock.Setup(a => a.StopApplication()).Callback(() => stoppingToken.Cancel());

            //Act
            var t = Task.Run(() =>
            {
                method.Invoke(serviceUnderTests, new[] { (object)stoppingToken.Token });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(10));
            stoppingToken.Cancel();


            //Assert
            Assert.True(fetchDate == (DateTimeOffset)property.GetValue(serviceUnderTests));

            covidDownloadingServiceMock.Verify(c => c.DownloadCovidByJsonAsync(), Times.Once);
            applicationLifetimeMock.Verify(a => a.StopApplication(), Times.Once);
            taskDelayProviderMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1),
                It.IsAny<CancellationToken>()));

            taskDelayProviderMock.VerifyNoOtherCalls();
            applicationLifetimeMock.VerifyNoOtherCalls();
        }
    }
}
