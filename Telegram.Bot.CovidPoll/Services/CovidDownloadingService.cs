using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.CovidPoll.Config;
using Telegram.Bot.CovidPoll.Exceptions;
using Telegram.Bot.CovidPoll.Helpers.Interfaces;
using Telegram.Bot.CovidPoll.Repositories.Interfaces;
using Telegram.Bot.CovidPoll.Services.Interfaces;

namespace Telegram.Bot.CovidPoll.Services
{
    public class CovidDownloadingService : ICovidDownloadingService
    {
        private readonly IOptions<CovidTrackingSettings> covidTrackingSettings;
        private readonly ICovidRepository covidRepository;
        private readonly IHttpClientFactory httpClient;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IBotPollResultSenderService botPollResultSender;
        private readonly IDateProvider dateProvider;

        public CovidDownloadingService(IOptions<CovidTrackingSettings> covidTrackingSettings, 
            ICovidRepository covidRepository, IHttpClientFactory httpClient, 
            IHostApplicationLifetime applicationLifetime, IBotPollResultSenderService botPollResultSender,
            IDateProvider dateProvider)
        {
            this.covidTrackingSettings = covidTrackingSettings;
            this.covidRepository = covidRepository;
            this.httpClient = httpClient;
            this.applicationLifetime = applicationLifetime;
            this.botPollResultSender = botPollResultSender;
            this.dateProvider = dateProvider;
        }

        public async Task<bool> DownloadCovidByJsonAsync()
        {
            var latestCovid = await covidRepository.FindLatestAsync();
            if (latestCovid != null && dateProvider.DateTimeUtcNow().Date <= latestCovid.Date)
                return true;

            var httpClient = this.httpClient.CreateClient();
            var response = await httpClient.GetAsync(covidTrackingSettings.Value.Url);
            if (!response.IsSuccessStatusCode)
                return false;

            var htmlContent = await response.Content.ReadAsStringAsync();
            try
            {
                dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(htmlContent);
                if (deserializedObject is ExpandoObject obj &&
                    ((IDictionary<string, object>)obj).ContainsKey("features"))
                {
                    var features = deserializedObject?.features as IEnumerable<dynamic>;
                    var attributes = ((features?
                        .FirstOrDefault(f => f is IDictionary<string, object> fDictionary &&
                            fDictionary.ContainsKey("attributes")))
                        as IDictionary<string, object>)?.FirstOrDefault();

                    var attributesData = attributes?.Value as IDictionary<string, object>;

                    var date = attributesData?.FirstOrDefault(a => a.Key.Equals("Data")).Value;
                    var cases = attributesData?
                        .FirstOrDefault(a => a.Key.Equals("ZAKAZENIA_DZIENNE")).Value;

                    if (date is not long unixTime)
                        throw new CovidParseException("date is not long");

                    if (cases is not IConvertible)
                        throw new CovidParseException("cases are not IConvertible");

                    var utcDateParsed = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).UtcDateTime;
                    if (latestCovid != null && latestCovid.Date >= utcDateParsed.Date)
                        return false;

                    await covidRepository.AddAsync(new Db.Covid
                    {
                        NewCases = Convert.ToInt32(cases),
                        Date = utcDateParsed
                    });
                    botPollResultSender.SendPredictionsResultsToChats();
                }
            }
            catch (Exception ex)
            {
                throw new CovidParseException("htmlContent is not string", ex);
            }
            return true;
        }

        public async Task<bool> DownloadCovidByRegexAsync()
        {
            var httpClient = this.httpClient.CreateClient();
            var latestCovid = await covidRepository.FindLatestAsync();
            if (latestCovid != null && DateTime.UtcNow.Date <= latestCovid.Date)
                return true;

            var response = await httpClient.GetAsync(covidTrackingSettings.Value.Url);
            if (response.IsSuccessStatusCode)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();

                var datePattern = "<div id=\"global-stats\">\\n<div class=\"global-stats\">\\n<p>" +
                    "Dane pochodzą z Ministerstwa Zdrowia,\\s*aktualne\\s*na\\s*:\\s*" +
                    "(\\d{2}.\\d{2}.\\d{4} \\d{2}:\\d{2})\\s*<a";

                var casesPattern ="<pre id=\"registerData\" class=\"hide\">{\"description\":\".*\"," +
                    "\"data\":\".*Cały kraj;([0-9]*).*<\\/pre>";

                var dateRegex = new Regex(datePattern, RegexOptions.IgnoreCase).Match(htmlContent);
                var casesRegex = new Regex(casesPattern, RegexOptions.IgnoreCase).Match(htmlContent);

                if (!dateRegex.Success || dateRegex.Groups.Count < 2)
                    throw new CovidParseException($"dataRegex failed");

                if (DateTime.TryParse(dateRegex.Groups[1].Value, CultureInfo.GetCultureInfo("pl-PL"),
                    DateTimeStyles.None, out var updateDate))
                {
                    if (latestCovid != null && latestCovid.Date >= updateDate.ToUniversalTime().Date)
                        return false;
                }
                else
                {
                    throw new CovidParseException(
                            $"DateTime parse exception, regexContent = {dateRegex.Groups[1].Value}");
                }

                if (!casesRegex.Success || casesRegex.Groups.Count < 2)
                    throw new CovidParseException($"casesRegex failed");

                if (int.TryParse(casesRegex.Groups[1].Value.Replace(" ", ""), out var newCases))
                {
                    await covidRepository.AddAsync(new Db.Covid
                    {
                        NewCases = newCases,
                        Date = DateTime.UtcNow.Date
                    });
                    botPollResultSender.SendPredictionsResultsToChats();
                    return true;
                }
                else
                {
                    throw new CovidParseException(
                        $"Cases parse exception, regexContent = {casesRegex.Groups[1].Value.Replace(" ", "")}");
                }
            }
            return false;
        }
    }
}
