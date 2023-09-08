using Discord;
using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Utilities;
using NSubstitute;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GrammarNazi.Tests;

public static class TestUtilities
{
    public static async Task TestDiscordNotAdminUser(IDiscordBotCommand command)
    {
        // Arrange
        const string replyMessage = "Only admins can use this command.";

        var channelMock = Substitute.For<IMessageChannel>();
        var user = Substitute.For<IGuildUser>();
        user.GuildPermissions.Returns(GuildPermissions.None);
        var message = Substitute.For<IMessage>();

        message.Author.Returns(user);
        message.Channel.Returns(channelMock);

        // Act
        await command.Handle(message);

        // Assert

        // Verify SendMessageAsync was called with the reply message "Only admins can use this command"
        await channelMock.Received().SendMessageAsync(Arg.Is<string>(s => s.Contains(replyMessage)), false, null, null, null, Arg.Is<MessageReference>(m => m.MessageId.Value == message.Id), null, null, null, MessageFlags.None);
    }

    public static async Task TestTelegramNotAdminUser(ITelegramBotCommand command, ITelegramBotClientWrapper telegramBotClientMock)
    {
        // Arrange
        const string replyMessage = "Only admins can use this command.";

        var message = new Message
        {
            Text = command.Command,
            From = new User { Id = 2 },
            Chat = new Chat
            {
                Id = 1,
                Type = ChatType.Group
            }
        };

        telegramBotClientMock.GetChatAdministratorsAsync(message.Chat.Id, default)
            .Returns(new ChatMember[0]);

        // Act
        await command.Handle(message);

        // Assert
        await telegramBotClientMock.Received().SendTextMessageAsync(message.Chat.Id, Arg.Is<string>(s => s.Contains(replyMessage)), default, default, default, default, default, message.MessageId, default, default, default);  
    }
}
