# grammar-nazi-bot
Bot that corrects spelling mistakes.

#### Telegram Bot
[@grammarNz_Bot](https://t.me/grammarNz_Bot): This bot analyzes each message that is sent in a Telegram chat, and if it finds any spelling or grammar errors, it replies to the message with its corrections using the asterisk symbol (*).

#### Twitter Bot
[@GrammarNBot](https://twitter.com/GrammarNBot) This Twitter bot analyzes the latest tweets of its followers, and if it finds any spelling or grammar errors, it will tweet a reply with its corrections using the asterisk symbol (*).

#### Discord Bot (Disabled, see #344)
[Add Bot to Server](https://discord.com/oauth2/authorize?client_id=800422872770150431&permissions=523328&scope=bot): This bot analyzes each message that is sent in a Discord channel, and if it finds any spelling or grammar errors, it replies to the message with its corrections using the asterisk symbol (*).

## Features
#### Twitter Bot
- Evaluates Tweets of followers.
- Multiple language support (English and Spanish).
- Follow back automatically.
#### Telegram and Discord Bot
- Configurable grammar analyzer algorithm or provider.
- Multiple language support (English and Spanish).
- Strictness Level.
- Whitelist Words.

Take a look at the [Telegram Bot](https://github.com/nminaya/grammar-nazi-bot/wiki/GrammarNazi-Telegram-Bot) and [Discord Bot](https://github.com/nminaya/grammar-nazi-bot/wiki/GrammarNazi-Discord-Bot) documentation.

## Solution Design
The solution design focuses on a basic Domain Driven Design techniques and implementation, while keeping the things as simple as possible but can be extended as needed. Multiple assemblies are used for separation of concerns to keep logic isolated from the other components. **.NET 5 C#** is the default framework and language for this application.

### Assembly Layers
-   **GrammarNazi.Domain**  - This assembly contains constants, entities and interfaces.
-   **GrammarNazi.Core**  - This assembly contains all domain implementations.
-   **GrammarNazi.Tests**  - This assembly contains unit test classes based on the xunit test framework.
-   **GrammarNazi.App**  - This assembly is the web-based application host.

## License

This project uses the following license: [MIT](LICENSE)
