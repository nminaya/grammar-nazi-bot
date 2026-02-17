using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace GrammarNazi.Tests.Services;

public class DiscordChannelConfigServiceTests
{
    [Fact]
    public async Task AddConfiguration_Should_Add_To_Repository_And_Cache()
    {
        // Arrange
        var repository = Substitute.For<IRepository<DiscordChannelConfig>>();
        var cache = Substitute.For<IMemoryCache>();
        var service = new DiscordChannelConfigService(repository, cache);
        var config = new DiscordChannelConfig { ChannelId = 123 };

        // Act
        await service.AddConfiguration(config);

        // Assert
        await repository.Received().Add(config);
        cache.Received().CreateEntry(Arg.Is<object>(k => k.ToString() == $"DiscordChannelConfig_{config.ChannelId}"));
    }

    [Fact]
    public async Task Delete_Should_Remove_From_Repository_And_Cache()
    {
        // Arrange
        var repository = Substitute.For<IRepository<DiscordChannelConfig>>();
        var cache = Substitute.For<IMemoryCache>();
        var service = new DiscordChannelConfigService(repository, cache);
        var config = new DiscordChannelConfig { ChannelId = 123 };

        // Act
        await service.Delete(config);

        // Assert
        await repository.Received().Delete(config);
        cache.Received().Remove(Arg.Is<object>(k => k.ToString() == $"DiscordChannelConfig_{config.ChannelId}"));
    }

    [Fact]
    public async Task GetConfigurationByChannelId_When_In_Cache_Should_Return_From_Cache()
    {
        // Arrange
        var repository = Substitute.For<IRepository<DiscordChannelConfig>>();
        var cache = Substitute.For<IMemoryCache>();
        var service = new DiscordChannelConfigService(repository, cache);
        var channelId = 123UL;
        var config = new DiscordChannelConfig { ChannelId = channelId };

        object outValue = config;
        cache.TryGetValue(Arg.Is<object>(k => k.ToString() == $"DiscordChannelConfig_{channelId}"), out Arg.Any<object>())
             .Returns(x =>
             {
                 x[1] = outValue;
                 return true;
             });

        // Act
        var result = await service.GetConfigurationByChannelId(channelId);

        // Assert
        Assert.Equal(config, result);
        await repository.DidNotReceive().GetFirst(Arg.Any<Expression<Func<DiscordChannelConfig, bool>>>());
    }

    [Fact]
    public async Task GetConfigurationByChannelId_When_Not_In_Cache_Should_Return_From_Repository_And_Cache_It()
    {
        // Arrange
        var repository = Substitute.For<IRepository<DiscordChannelConfig>>();
        var cache = Substitute.For<IMemoryCache>();
        var service = new DiscordChannelConfigService(repository, cache);
        var channelId = 123UL;
        var config = new DiscordChannelConfig { ChannelId = channelId };

        cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);
        repository.GetFirst(Arg.Any<Expression<Func<DiscordChannelConfig, bool>>>()).Returns(config);

        // Act
        var result = await service.GetConfigurationByChannelId(channelId);

        // Assert
        Assert.Equal(config, result);
        await repository.Received().GetFirst(Arg.Any<Expression<Func<DiscordChannelConfig, bool>>>());
        cache.Received().CreateEntry(Arg.Is<object>(k => k.ToString() == $"DiscordChannelConfig_{channelId}"));
    }

    [Fact]
    public async Task Update_Should_Update_Repository_And_Cache()
    {
        // Arrange
        var repository = Substitute.For<IRepository<DiscordChannelConfig>>();
        var cache = Substitute.For<IMemoryCache>();
        var service = new DiscordChannelConfigService(repository, cache);
        var config = new DiscordChannelConfig { ChannelId = 123 };

        // Act
        await service.Update(config);

        // Assert
        await repository.Received().Update(config, Arg.Any<Expression<Func<DiscordChannelConfig, bool>>>());
        cache.Received().CreateEntry(Arg.Is<object>(k => k.ToString() == $"DiscordChannelConfig_{config.ChannelId}"));
    }
}
