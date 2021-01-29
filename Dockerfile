#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim AS build
WORKDIR /src
COPY ["Telegram.Bot.CovidPoll/Telegram.Bot.CovidPoll.csproj", "Telegram.Bot.CovidPoll/"]
RUN dotnet restore "Telegram.Bot.CovidPoll/Telegram.Bot.CovidPoll.csproj"
COPY . .
WORKDIR "/src/Telegram.Bot.CovidPoll"
RUN dotnet build "Telegram.Bot.CovidPoll.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Telegram.Bot.CovidPoll.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Telegram.Bot.CovidPoll.dll"]