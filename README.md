# grammar-nazi-bot
Telegram bot that corrects spelling mistakes.

This bot analyzes each message that is sent in a Telegram chat, and if it finds any spelling or grammar errors, it responds to the message with its correction using the asterisk symbol (*).

### Features
- Configurable grammar analyzer algorithm or provider.

### Solution Design
The solution design focuses on a basic Domain Driven Design techniques and implementation, while keeping the things as simple as possible but can be extended as needed. Multiple assemblies are used for separation of concerns to keep logic isolated from the other components. **Microsoft .NET Core 3.1 C#** is the default framework and language for this application.

### Assembly Layers
-   **GrammarNazi.Core**  - This assembly contains all domain implementations.
-   **GrammarNazi.Domain**  - This assembly contains constants, entities and interfaces.
-   **GrammarNazi.Tests**  - This assembly contains unit test classes based on the xunit test framework.
-   **GrammarNazi.Integration.Tests**  - This assembly contains integration test classes based on the xunit test framework.     
-   **GrammarNazi.App**  - This assembly is the web-based application host.

## Features to implement
This project is still under development, there are a lot of features to implement.
- Multiple language support.
- Unit test and Integration Test projects.
- Trained ML model that improves the internal algorithm corrections.

## Run the Project
To run this project locally you just need to clone the repo and run the **GrammarNazi.App** project. But, before that you should [create a Telegram Bot](https://core.telegram.org/bots#6-botfather) with [BotFather](https://t.me/BotFather) and set the Token as an environment variable named **TELEGRAM_API_KEY**. You can set the environment variable in [launchSettings.json](https://github.com/nminaya/grammar-nazi-bot/blob/master/GrammarNazi.App/Properties/launchSettings.json).
