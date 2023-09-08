using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Entities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Services;
using GrammarNazi.Domain.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Xunit;

namespace GrammarNazi.Tests.Utilities;

public class TelegramUpdateHandlerTests
{
    [Fact]
    public async Task HandleUpdate_NonSupportedUpdateTypeReceived_Should_DoNothing()
    {
        // Arrange
        var telegramBotMock = Substitute.For<ITelegramBotClient>();
        var loggerMock = Substitute.For<ILogger<TelegramUpdateHandler>>();

        var update = new Update
        {
            ChatMember = new ChatMemberUpdated()
        };

        var handler = new TelegramUpdateHandler(null, null, loggerMock);

        // Act
        await handler.HandleUpdateAsync(telegramBotMock, update, default);

        // Assert

        // Verify LogInformation was called
        var numberOfCalls = loggerMock.ReceivedCalls()
                .Select(call => call.GetArguments())
                .Count(callArguments => ((LogLevel)callArguments[0]).Equals(LogLevel.Information));

        Assert.Equal(1, numberOfCalls);
    }

    [Fact]
    public async Task HandleUpdate_MessageReceivedNotTextType_Should_DoNothing()
    {
        // Arrange
        var telegramBotMock = Substitute.For<ITelegramBotClient>();
        var chatConfigServiceMock = Substitute.For<IChatConfigurationService>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        var update = new Update
        {
            Message = new Message
            {
                Audio = new Audio(),
                Chat = new Chat { Id = 1 }
            }
        };

        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(IChatConfigurationService)).Returns(chatConfigServiceMock);

        var handler = new TelegramUpdateHandler(serviceScopeFactory, null, null);

        // Act
        await handler.HandleUpdateAsync(telegramBotMock, update, default);

        // Assert
        await chatConfigServiceMock.DidNotReceive().GetConfigurationByChatId(update.Message.Chat.Id);
    }

    [Fact]
    public async Task HandleUpdate_MessageReceived_Should_GetCorrections()
    {
        // Arrange
        var telegramBotMock = Substitute.For<ITelegramBotClient>();
        var chatConfigServiceMock = Substitute.For<IChatConfigurationService>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var loggerMock = Substitute.For<ILogger<TelegramUpdateHandler>>();
        var grammarService = Substitute.For<IGrammarService>();

        var update = new Update
        {
            Message = new Message
            {
                Text = "My Text",
                Chat = new Chat { Id = 1 }
            }
        };

        chatConfigServiceMock.GetConfigurationByChatId(update.Message.Chat.Id)
            .Returns(new ChatConfiguration());

        grammarService.GetCorrections("My Text")
            .Returns(new GrammarCheckResult(null));

        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(IChatConfigurationService)).Returns(chatConfigServiceMock);
        serviceProvider.GetService(typeof(IEnumerable<IGrammarService>)).Returns(new[] { grammarService });

        var handler = new TelegramUpdateHandler(serviceScopeFactory, null, loggerMock);

        // Act
        await handler.HandleUpdateAsync(telegramBotMock, update, default);

        // Assert
        await chatConfigServiceMock.Received().GetConfigurationByChatId(update.Message.Chat.Id);
        await grammarService.Received().GetCorrections("My Text");
    }
}
