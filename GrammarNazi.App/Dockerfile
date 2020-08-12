#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["GrammarNazi.App/GrammarNazi.App.csproj", "GrammarNazi.App/"]
RUN dotnet restore "GrammarNazi.App/GrammarNazi.App.csproj"
COPY . .
WORKDIR "/src/GrammarNazi.App"
RUN dotnet build "GrammarNazi.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrammarNazi.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GrammarNazi.App.dll"]