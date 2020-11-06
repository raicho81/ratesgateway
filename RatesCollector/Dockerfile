#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["RatesCollector/RatesCollector.csproj", "RatesCollector/"]
RUN dotnet restore "RatesCollector/RatesCollector.csproj"
COPY . .
WORKDIR "/src/RatesCollector"
RUN dotnet build "RatesCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RatesCollector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY RatesCollector/settings.json .
ENTRYPOINT ["dotnet", "RatesCollector.dll"]