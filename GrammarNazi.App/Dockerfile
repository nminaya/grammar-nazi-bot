#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["GrammarNazi.App/GrammarNazi.App.csproj", "GrammarNazi.App/"]
RUN dotnet restore "GrammarNazi.App/GrammarNazi.App.csproj"

# Copy everything else and build
COPY . ./
RUN dotnet publish "GrammarNazi.App.csproj" -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
CMD dotnet GrammarNazi.App.dll