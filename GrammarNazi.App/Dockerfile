#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["GrammarNazi.App/GrammarNazi.App.csproj", "GrammarNazi.App/"]
RUN dotnet restore "GrammarNazi.App/GrammarNazi.App.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/GrammarNazi.App"
RUN dotnet build "GrammarNazi.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrammarNazi.App.csproj" -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /src/out .
CMD dotnet GrammarNazi.App.dll