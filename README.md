<div align="center">

# Project status

|Build|Unit Tests|Integration Tests|
|------|-------|-------|
|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Build&jobName=Execute%20build%20and%20push%20to%20DockerHub)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Unit%20tests&jobName=Execute%20XUnit%20tests)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|[![Build Status](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_apis/build/status/Build%20and%20tests?branchName=main&stageName=Integration%20tests&jobName=Execute%20integration%20tests)](https://dev.azure.com/bartlomiejkwiatkowski00/Telegram.Bot.CovidPoll/_build/latest?definitionId=13&branchName=main)|

</div>

## What it is for?
I wanted to create a useful system to predict covid cases for the next day. It's not something like ML, but it depends on users votes.

### How it works
You must add this bot to your group. Each day the bot will send the poll with options based on the last number of covid cases. Users have limited time to vote. When it ends, the poll will be closed and prediction number based on votes from all groups will be sent to each of them. On the next day, the bot will check if there is new data in API (until it changes). When the data is updated, the bot will send the information about the number of new covid cases and voting results.

### Some details
* It's important to know that all group members have their own vote ratio, even when one member is in many groups, he has individual ratio in each group.
* Vote ratio is used to predict the number of covid cases, which is calculated by weighted average of ratio and votes.
* Users can also vote by using a command /vote {custom_option} without the need to tick an answer in the poll.

## Installation
The latest version of this bot is always on DockerHub (connected by Azure Pipelines to this repository):
```
docker pull kwiatek1100/telegram.bot.covidpoll:latest
```

## Usage
It is very simple to start working with bot on your own hosting.

### Prepare database and place for logging
This project is using [MongoDb](https://hub.docker.com/_/mongo) and [datalust/seq](https://hub.docker.com/r/datalust/seq) for logging.

1. Create [bot token](https://core.telegram.org/bots#3-how-do-i-create-a-bot) (for **BotSettings__Token**).
2. Check your AdminUserId in ```https://api.telegram.org/bot{your_key}/getUpdates```. (You need to send at least one message to the bot.)
2. Run Seq, then create an API key (for **Seq__ApiKey** and **Seq__ServerUrl**).
3. Run MongoDb and fill connection string with your data (for **MongoSettings__ConnectionString**).
```
mongodb://username:password@ip_with_port
```
5. The bot is integrated with this [API](https://koronawirus-api.herokuapp.com/api/covid19/daily) (for **CovidTrackingSettings__Url**). But you can write your own implementation for another API. :)
6. Run Telegram.Bot.CovidPoll with changed environment variables.



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
7. Of course, you can also change time, when polls are starting and closing or when the bot is begins to fetch data.
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
