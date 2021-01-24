using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using Telegram.Bot.CovidPoll.xUnit.Fixtures;
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
        private readonly Mock<ITaskDelayHelper> taskDelayHelperMock;
        private readonly CovidTrackingHostedService serviceUnderTests;

        public CovidTrackingHostedServiceTests(AppSettingsFixture appSettingsFixture)
        {
            this.covidTrackingSettingsMock = new Mock<IOptions<CovidTrackingSettings>>();
            this.applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            this.logMock = new Mock<ILogger<CovidTrackingHostedService>>();
            this.covidDownloadingServiceMock = new Mock<ICovidDownloadingService>();
            this.taskDelayHelperMock = new Mock<ITaskDelayHelper>();
            this.serviceUnderTests = new CovidTrackingHostedService(covidTrackingSettingsMock.Object,
                applicationLifetimeMock.Object, logMock.Object, covidDownloadingServiceMock.Object,
                taskDelayHelperMock.Object);
        }
        #endregion

        [Fact]
        public async Task ExecuteAsync()
        {
            //Arrange
            var method = serviceUnderTests.GetType()
                .GetMethod("WorkerAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var property = serviceUnderTests.GetType()
                .GetProperty("FetchDate", BindingFlags.NonPublic | BindingFlags.Instance);

            var fetchDate = DateTimeOffset.UtcNow;
            property.SetValue(serviceUnderTests,
                Convert.ChangeType(fetchDate, property.PropertyType));

            covidDownloadingServiceMock.Setup(c => c.DownloadCovidByJsonAsync()).ReturnsAsync(true);

            var stoppingToken = new CancellationTokenSource();
            taskDelayHelperMock.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(500, stoppingToken.Token));

            //Act
            var t = Task.Run(() => 
            {
                method.Invoke(serviceUnderTests, new[] { (object) default(CancellationToken) });
            }, stoppingToken.Token);

            await Task.Delay(TimeSpan.FromSeconds(2));
            stoppingToken.Cancel();


            //Assert
            covidDownloadingServiceMock.Verify(c => c.DownloadCovidByJsonAsync(), Times.Once);;
            Assert.True(fetchDate.AddDays(1) == (DateTimeOffset)property.GetValue(serviceUnderTests));

            taskDelayHelperMock.Verify(t => t.Delay(TimeSpan.FromSeconds(1), It.IsAny<CancellationToken>()));
            applicationLifetimeMock.VerifyNoOtherCalls();
        }
    }
}
