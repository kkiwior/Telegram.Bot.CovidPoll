<div align="center">

# Project status

|Build|Unit Tests|Integration Tests|
|------|-------|-------|
|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Build&jobName=Execute%20build%20and%20push%20to%20DockerHub)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Unit%20tests&jobName=Execute%20XUnit%20tests)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Integration%20tests&jobName=Execute%20integration%20tests)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|

</div>

## What it is for?
I wanted to create some usefull system to predict covid cases for the next day. It's not something like ML, but it depends on users votes.

### How it works
You must add this bot to your group. Then once a day bot will send poll with options based on the last number of new covid cases. Users have time for vote, when time ends polls will be closed and prediction number based on votes from all groups will be send to them. In the next day, bot will check if there is new data in API (until it effect). When there will be new data, bot will send information about new covid cases and information who voted properly.

### Some details
* It's important to know that, all group members have own vote ratio, even when one member is in many groups, he has separated ratio in each of groups.
* Vote ratio is for covid cases prediction, covid predictions are calculated by weighted average.
* Users have an opportunity to vote outside the numbers in poll. They have for that command /vote {here_covid_cases}.

## Installation
The latest version of this bot is always in DockerHub (connected by Azure Pipelines to this repository):
```
docker pull kwiatek1100/telegram.bot.covidpoll:latest
```

## Usage
It is very simple to start working with bot on your own hosting.

### Prepare database and place for logging
This project is using [MongoDb](https://hub.docker.com/_/mongo) and [datalust/seq](https://hub.docker.com/r/datalust/seq) for logging.

1. Create [bot token](https://core.telegram.org/bots#3-how-do-i-create-a-bot) (for **BotSettings__Token**).
2. Check your AdminUserId in ```https://api.telegram.org/bot{your_key}/getUpdates```. You need send to bot at least one message and check your userId from this message here.
2. Run Seq, then create an API key (for **Seq__ApiKey** and **Seq__ServerUrl**).
3. Run MongoDb and fill this connection string with your data (for **MongoSettings__ConnectionString**).
```
mongodb://username:password@ip_with_port
```
5. Bot is integrated with this [API]() (for **CovidTrackingSettings__Url**). But you can write your own implementation for another API. :)
6. Run Telegram.Bot.CovidPoll replacing values with yours.



```
docker run -ti -d \
-e BotSettings__Token="telegram_bot_token" \
-e BotSettings__AdminUserId="telegram_admin_user_id" \
-e MongoSettings__ConnectionString="mongo_connection_string" \
-e MongoSettings__DbName="db_name" \
-e CovidTrackingSettings__Url="covid_api_url" \
-e Seq__ApiKey="seq_api_key" \
-e Seq__ServerUrl="seq_url" \
kwiatek1100/telegram.bot.covidpoll
```
7. Of course you can also change when polls are starting and closing or when bot is starting to fetching data.
```
-e BotSettings__PollsStartHour=""
-e BotSettings__PollsEndHour=""
-e CovidTrackingSettings__FetchDataHour=""
```

## License
* Project is under [MIT](https://github.com/bartlomiejkwiatkowski/Telegram.Bot.CovidPoll/blob/main/LICENSE) license

* Dependencies can have own licenses:
```
https://www.nuget.org/packages/FluentAssertions/
https://www.nuget.org/packages/Microsoft.NET.Test.SDK
https://www.nuget.org/packages/Moq/
https://www.nuget.org/packages/xunit/
https://www.nuget.org/packages/xunit.runner.visualstudio
https://www.nuget.org/packages/Microsoft.VisualStudio.Azure.Containers.Tools.Targets/
https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/
https://www.nuget.org/packages/Microsoft.Extensions.Hosting
https://www.nuget.org/packages/Microsoft.Extensions.Http/
https://www.nuget.org/packages/mongodb.driver
https://www.nuget.org/packages/Seq.Extensions.Logging/
https://www.nuget.org/packages/Telegram.Bot/
```
