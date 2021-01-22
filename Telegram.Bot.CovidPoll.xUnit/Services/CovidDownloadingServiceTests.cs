using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Db;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Repositories;
using Telegram.Bot.CovidPoll.Services;
using Telegram.Bot.CovidPoll.Services.Interfaces;
using Telegram.Bot.CovidPoll.xUnit.Fixtures;
using Xunit;

namespace Telegram.Bot.CovidPoll.xUnit.Services
{
    public class CovidDownloadingServiceTests : IClassFixture<AppSettingsFixture>
    {
        #region CONFIG
        private HttpClient httpClientMock;
        private readonly Mock<HttpMessageHandler> httpMessageHandlerMock;
        private readonly AppSettingsFixture appSettingsFixture;
        private readonly ICovidDownloadingService serviceUnderTests;
        private readonly Mock<ICovidRepository> covidRepositoryMock;
        private readonly Mock<IHttpClientFactory> httpClientFactoryMock;
        private readonly Mock<IHostApplicationLifetime> applicationLifetimeMock;
        private readonly Mock<IBotPollResultSenderService> botPollResultSenderMock;
        private readonly string httpMessageContent = "{\"features\": [{\"attributes\":{\"Data\": 1611221413886,\"ZAKAZENIA_DZIENNE\": 7152}}]}";
        private readonly DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(1611221413886).UtcDateTime;
        private readonly int cases = 7152;

        public CovidDownloadingServiceTests(AppSettingsFixture appSettingsFixture)
        {
            this.httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            this.appSettingsFixture = appSettingsFixture;
            this.covidRepositoryMock = new Mock<ICovidRepository>();
            this.httpClientFactoryMock = new Mock<IHttpClientFactory>();
            this.applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            this.botPollResultSenderMock = new Mock<IBotPollResultSenderService>();

            this.serviceUnderTests = new CovidDownloadingService(appSettingsFixture.CovidTrackingSettings,
                covidRepositoryMock.Object, httpClientFactoryMock.Object, applicationLifetimeMock.Object,
                botPollResultSenderMock.Object);
        }
        #endregion

        [Fact]
        public async Task DownloadCovidByJsonAsync_StatusCodeIsOkAndCasesInDbAreFromYesterday_ShouldAddToDbAndReturnTrueAndSendResultsToChats()
        {
            //Arrange
            covidRepositoryMock.Setup(c => c.FindLatestAsync())
                .ReturnsAsync(new Covid() { Date = date.AddDays(-1) });

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(httpMessageContent)
                });

            httpClientMock = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>()))
                .Returns(httpClientMock);

            //Act
            var result = await serviceUnderTests.DownloadCovidByJsonAsync();

            //Assert
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get
                  && req.RequestUri == new Uri(appSettingsFixture.CovidTrackingSettings.Value.Url)),
                ItExpr.IsAny<CancellationToken>());

            covidRepositoryMock.Verify(c => c.FindLatestAsync(), Times.Once);
            covidRepositoryMock.Verify(c => 
                c.AddAsync(It.Is<Covid>(m => m.Date == date && m.NewCases == cases)), Times.Once);
            botPollResultSenderMock.Verify(b => b.SendPredictionsResultsToChats(), Times.Once);

            covidRepositoryMock.VerifyNoOtherCalls();
            botPollResultSenderMock.VerifyNoOtherCalls();
            httpMessageHandlerMock.VerifyNoOtherCalls();
            Assert.True(result);
        }

        [Fact]
        public async Task DownloadCovidByJsonAsync_StatusCodeIsOkAndCasesInDbAreNotUpToDateAndJsonDataIsInvalid_ShouldThrowCovidCalculateException()
        {
            //Arrange
            covidRepositoryMock.Setup(c => c.FindLatestAsync())
                .ReturnsAsync(new Covid() { Date = date.AddDays(-1) });

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{")
                });

            httpClientMock = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>()))
                .Returns(httpClientMock);

            //Act && Assert
            await Assert.ThrowsAsync<CovidParseException>(() => 
                serviceUnderTests.DownloadCovidByJsonAsync());

            //Assert
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get
                  && req.RequestUri == new Uri(appSettingsFixture.CovidTrackingSettings.Value.Url)),
                ItExpr.IsAny<CancellationToken>());

            covidRepositoryMock.Verify(c => c.FindLatestAsync(), Times.Once);

            covidRepositoryMock.VerifyNoOtherCalls();
            botPollResultSenderMock.VerifyNoOtherCalls();
            httpMessageHandlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DownloadCovidByJsonAsync_CasesInDbAreUpToDate_ShouldReturnTrue()
        {
            //Arrange
            covidRepositoryMock.Setup(c => c.FindLatestAsync())
                .ReturnsAsync(new Covid() { Date = DateTime.UtcNow });

            //Act
            var result = await serviceUnderTests.DownloadCovidByJsonAsync();


            //Assert
            covidRepositoryMock.Verify(c => c.FindLatestAsync(), Times.Once);

            covidRepositoryMock.VerifyNoOtherCalls();
            botPollResultSenderMock.VerifyNoOtherCalls();
            httpMessageHandlerMock.VerifyNoOtherCalls();

            Assert.True(result);
        }

        [Fact]
        public async Task DownloadCovidByJsonAsync_StatusCodeIsNotOkAndCasesInDbAreNotUpToDate_ShouldReturnFalse()
        {
            //Arrange
            covidRepositoryMock.Setup(c => c.FindLatestAsync())
                .ReturnsAsync(new Covid() { Date = date.AddDays(-1) });

            httpMessageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = System.Net.HttpStatusCode.InternalServerError,
                   Content = new StringContent(httpMessageContent)
               });

            httpClientMock = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>()))
                .Returns(httpClientMock);

            //Act
            var result = await serviceUnderTests.DownloadCovidByJsonAsync();


            //Assert
            covidRepositoryMock.Verify(c => c.FindLatestAsync(), Times.Once);
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get
                  && req.RequestUri == new Uri(appSettingsFixture.CovidTrackingSettings.Value.Url)),
                ItExpr.IsAny<CancellationToken>());

            covidRepositoryMock.VerifyNoOtherCalls();
            botPollResultSenderMock.VerifyNoOtherCalls();
            httpMessageHandlerMock.VerifyNoOtherCalls();

            Assert.False(result);
        }

        [Fact]
        public async Task DownloadCovidByJsonAsync_StatusCodeIsOkAndCasesInDbAreNotUpToDateAndCasesOnPageAreNotUpToDate_ShouldReturnFalse()
        {
            //Arrange
            covidRepositoryMock.Setup(c => c.FindLatestAsync())
                .ReturnsAsync(new Covid() { Date = date.AddDays(-1) });

            var pageDate = 
                "{\"features\": [{\"attributes\":{\"Data\": " +
                ((DateTimeOffset) date.AddDays(-1)).ToUnixTimeSeconds() + 
                ",\"ZAKAZENIA_DZIENNE\": 7152}}]}";

            httpMessageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = System.Net.HttpStatusCode.OK,
                   Content = new StringContent(pageDate)
               });

            httpClientMock = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>()))
                .Returns(httpClientMock);

            //Act
            var result = await serviceUnderTests.DownloadCovidByJsonAsync();


            //Assert
            covidRepositoryMock.Verify(c => c.FindLatestAsync(), Times.Once);
            httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Get
                  && req.RequestUri == new Uri(appSettingsFixture.CovidTrackingSettings.Value.Url)),
                ItExpr.IsAny<CancellationToken>());

            covidRepositoryMock.VerifyNoOtherCalls();
            botPollResultSenderMock.VerifyNoOtherCalls();
            httpMessageHandlerMock.VerifyNoOtherCalls();

            Assert.False(result);
        }
    }
}
