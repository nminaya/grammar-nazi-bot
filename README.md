# grammar-nazi-bot
Bot that corrects spelling mistakes.

#### Telegram Bot
[@grammarNz_Bot](https://t.me/grammarNz_Bot): This bot analyzes each message that is sent in a Telegram chat, and if it finds any spelling or grammar errors, it replies to the message with its corrections using the asterisk symbol (*).

#### Twitter Bot
#### Twitter Bot (Disabled, see [https://twitter.com/GrammarNBot/status/1670853991511539714](https://twitter.com/GrammarNBot/status/1670853991511539714))
[@GrammarNBot](https://twitter.com/GrammarNBot) Analyzes the tweet you're replying to mentioning the bot, and if it finds any spelling or grammar errors, it will tweet a reply with its corrections using the asterisk symbol (*).

#### Discord Bot
[Add Bot to Server](https://discord.com/oauth2/authorize?client_id=800422872770150431&permissions=523328&scope=bot): This bot analyzes each message that is sent in a Discord channel, and if it finds any spelling or grammar errors, it replies to the message with its corrections using the asterisk symbol (*).

## Features
#### Twitter Bot
- Evaluates Tweets where the bot is mentioned.
- Multiple language support (English and Spanish).
- Follow back automatically.
#### Telegram and Discord Bot
- Configurable grammar analyzer algorithm or provider.
- Multiple language support (English and Spanish).
- Strictness Level.
- Whitelist Words.

Take a look at the [Telegram Bot](https://github.com/nminaya/grammar-nazi-bot/wiki/GrammarNazi-Telegram-Bot) and [Discord Bot](https://github.com/nminaya/grammar-nazi-bot/wiki/GrammarNazi-Discord-Bot) documentation.

## Solution Design
The solution design focuses on a basic Domain Driven Design techniques and implementation, while keeping the things as simple as possible but can be extended as needed. Multiple assemblies are used for separation of concerns to keep logic isolated from the other components. **.NET 10 C#** is the default framework and language for this application.

### Assembly Layers
-   **GrammarNazi.Domain**  - This assembly contains constants, entities and interfaces.
-   **GrammarNazi.Core**  - This assembly contains all domain implementations.
-   **GrammarNazi.Tests**  - This assembly contains unit test classes based on the xunit test framework.
-   **GrammarNazi.App**  - This assembly is the web-based application host.

## License
This project uses the following license: [MIT](LICENSE)
