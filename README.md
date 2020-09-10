# grammar-nazi-bot
Bot that corrects spelling mistakes.

#### Telegram Bot
[@grammarNz_Bot](https://t.me/grammarNz_Bot): This bot analyzes each message that is sent in a Telegram chat, and if it finds any spelling or grammar errors, it responds to the message with its corrections using the asterisk symbol (*).

#### Twitter Bot
[@GrammarNazi_Bot](https://twitter.com/GrammarNazi_Bot) This Twitter bot analyzes the latest tweets of its followers, and if it finds any spelling or grammar errors, it will tweet a reply with its corrections using the asterisk symbol (*).

## Features
#### Telegram Bot
- Configurable grammar analyzer algorithm or provider.
- Multiple language support (English and Spanish).
#### Twitter Bot
- Evaluates Tweets of followers.
- Multiple language support (English and Spanish).
## Solution Design
The solution design focuses on a basic Domain Driven Design techniques and implementation, while keeping the things as simple as possible but can be extended as needed. Multiple assemblies are used for separation of concerns to keep logic isolated from the other components. **Microsoft .NET Core 3.1 C#** is the default framework and language for this application.

### Assembly Layers
-   **GrammarNazi.Core**  - This assembly contains all domain implementations.
-   **GrammarNazi.Domain**  - This assembly contains constants, entities and interfaces.
-   **GrammarNazi.Tests**  - This assembly contains unit test classes based on the xunit test framework.
-   **GrammarNazi.App**  - This assembly is the web-based application host.

## Features to implement
This project is still under development, there are a lot of features to implement.
- Accuracy percentage for possible corrections.
- Trained ML model that improves the internal algorithm corrections.
#### Telegram Bot
- Configurable [Grammar rules](https://community.languagetool.org/rule/)
#### Twitter Bot
- Publish tweets with statistics based on corrected tweets.

## Run the Project
To run this project locally you just need to clone the repo and run the **GrammarNazi.App** project. 
This project interacts with Twitter and Telegram, so you will need a [Telegram API Token](https://core.telegram.org/bots#6-botfather) for the Telegram Bot and [Twitter Credentials](https://developer.twitter.com/en/apply-for-access) for the Twitter Bot.

Then you can set the environment variables in [launchSettings.json](https://github.com/nminaya/grammar-nazi-bot/blob/master/GrammarNazi.App/Properties/launchSettings.json).
