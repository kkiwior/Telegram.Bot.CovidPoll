#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["Telegram.Bot.CovidPoll/Telegram.Bot.CovidPoll.csproj", "Telegram.Bot.CovidPoll/"]
RUN dotnet restore "Telegram.Bot.CovidPoll/Telegram.Bot.CovidPoll.csproj"
COPY . .
WORKDIR "/src/Telegram.Bot.CovidPoll"
RUN dotnet build "Telegram.Bot.CovidPoll.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Telegram.Bot.CovidPoll.csproj" -c Release -o /app/publish

FROM base AS final
ARG BotSettings__Token
ARG BotSettings__AdminUserId
ARG MongoSettings__ConnectionString
ARG MongoSettings__DbName
ARG CovidTrackingSettings_Url
ARG Seq__ApiKey
ARG Seq__ServerUrl

ENV BotSettings__Token $BotSettings__Token
ENV BotSettings__AdminUserId $BotSettings__AdminUserId
ENV MongoSettings__ConnectionString $MongoSettings__ConnectionString
ENV MongoSettings__DbName $MongoSettings__DbName
ENV CovidTrackingSettings_Url $CovidTrackingSettings_Url
ENV Seq__ApiKey $Seq__ApiKey
ENV Seq__ServerUrl $Seq__ServerUrl

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Telegram.Bot.CovidPoll.dll"]