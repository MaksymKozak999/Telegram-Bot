# Telegram Bot

A multifunctional Telegram bot built with **C# and .NET** using the **Telegram Bot API**.

The project was created as a personal project to improve my C# skills and explore **Telegram bot development, asynchronous programming, API integration, cryptographically secure random generation, and state management**.

## Features

### Random Generator

Generate different types of random values:

* Random numbers
* Random characters (`a-z`)
* Coin flips
* Custom number ranges

Custom ranges can be generated with:

```text
/roll <min> <max>
```

Example:

```text
/roll 10 50
```

The random generator uses `RandomNumberGenerator` instead of the standard `System.Random` for cryptographically secure random values.

### Password Generator

Generate customizable passwords with:

* Configurable length
* Uppercase letters
* Numbers
* Symbols
* Secure random character selection
* Fisher–Yates shuffling

Password settings are stored separately for each Telegram chat.

### URL Shortener

Shorten long URLs directly from Telegram.

The bot supports both:

```text
/url https://example.com
```

and an interactive mode where the bot waits for the user to send a URL.

The project communicates with an external URL-shortening API using `HttpClient`.

### Profile

The profile page displays:

* Telegram name
* Username
* Telegram ID
* Current random generator mode
* Password generator settings

### Settings

The bot provides interactive settings using Telegram inline keyboards.

Users can:

* Change random generator mode
* Enable custom random ranges
* Change password length
* Enable/disable uppercase letters
* Enable/disable numbers
* Enable/disable symbols
* Reset password settings

## Technologies

* **C#**
* **.NET**
* **Telegram.Bot**
* **Telegram Bot API**
* **Microsoft.Extensions.Configuration**
* **HttpClient**
* **ConcurrentDictionary**
* **async / await**
* **System.Security.Cryptography**
* **Git / GitHub**

## Technical Highlights

### Cryptographically Secure Random Generation

The project uses:

```csharp
RandomNumberGenerator
```

to generate random values.

This is used for:

* random numbers
* random characters
* coin flips
* password generation

The implementation also supports both single-bound and custom-range random generation.

### Per-Chat State

The bot maintains separate state for each Telegram chat using thread-safe `ConcurrentDictionary` instances.

For example:

```csharp
ConcurrentDictionary<long, string>
```

is used to store the selected random generator mode.

Password configurations are also maintained separately for each chat.

### Telegram Keyboards

The bot uses both:

* Reply keyboards
* Inline keyboards
* Callback queries

This allows users to interact with most functionality without manually entering commands.

### Asynchronous API Communication

The application uses asynchronous operations for Telegram API calls and external HTTP requests:

```csharp
async / await
```

This keeps network operations non-blocking.

## Project Structure

```text
Telegram-Bot/
├── packages/
├── secondtgbot/
│   ├── Program.cs
│   ├── CharsSets.cs
│   ├── CryptoRandom.cs
│   ├── PasswordConfig.cs
│   ├── UrlShortenerService.cs
│   └── ...
├── .gitignore
├── LICENSE.txt
└── secondtgbot.slnx
```

## Getting Started

### Requirements

* .NET SDK
* Telegram account
* Telegram bot created with [@BotFather](https://t.me/BotFather)

### Clone the repository

```bash
git clone https://github.com/MaksymKozak999/Telegram-Bot.git
cd Telegram-Bot
```

### Configure the bot

Create an `appsettings.json` file and add your Telegram Bot API token:

```json
{
  "TelegramBotToken": "YOUR_TELEGRAM_BOT_TOKEN"
}
```

The application reads the token from configuration when starting the bot.

> **Important:** Never commit your real Telegram bot token to GitHub.

### Restore dependencies

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

After starting the application, open the bot in Telegram and use:

```text
/start
```

The bot will initialize its main menu and start receiving updates through Telegram polling.

## Available Commands

| Command             | Description                          |
| ------------------- | ------------------------------------ |
| `/start`            | Start the bot and open the main menu |
| `/help`             | Display available functionality      |
| `/roll <min> <max>` | Generate a number in a custom range  |
| `/url <URL>`        | Shorten a URL                        |

Most other functionality is accessible through the Telegram keyboard interface.

## What I Practiced

This project helped me develop practical experience with:

* C# and Object-Oriented Programming
* .NET application development
* Telegram Bot API
* asynchronous programming
* HTTP API integration
* `HttpClient`
* cryptographically secure random generation
* thread-safe collections
* state management
* configuration management
* Telegram inline and reply keyboards
* callback queries
* Git and GitHub
* error handling

## Future Improvements

* [ ] Refactor command handling into separate classes
* [ ] Add unit tests
* [ ] Improve error handling and logging
* [ ] Replace in-memory state with persistent storage
* [ ] Improve the profile system
* [ ] Add more bot functionality
* [ ] Improve API response handling
* [ ] Add Docker support
* [ ] Deploy the bot for 24/7 operation

## License

This project is licensed under the **MIT License**.

## Author

**Maksym Kozak**

[GitHub](https://github.com/MaksymKozak999)
