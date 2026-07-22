using Microsoft.Extensions.Configuration;
using secondtgbot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyTelegramBot
{
    class Program
    {
        // Make the bot token and client static fields of the class
        private static ITelegramBotClient _botClient;
        
        // TRACKING STATE: Stores whether a specific Chat ID is set to "char" mode or "number" mode (default)
        private static ConcurrentDictionary<long, string> _chatModes = new ConcurrentDictionary<long, string>();

        // True if bot waiting for link
        private static ConcurrentDictionary<long, bool> _awaitingUrlInput = new ConcurrentDictionary<long, bool>();

        private static ConcurrentDictionary<long, PasswordConfig> _passwordConfigs = new ConcurrentDictionary<long, PasswordConfig>();


        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            // Initialize the client (replace with your token)

            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();

            string token = config["TelegramBotToken"];
            _botClient = new TelegramBotClient(token);

            using (var cts = new CancellationTokenSource())
            {
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new UpdateType[0] // Receive all update types
                };

                var me = await _botClient.GetMe(cts.Token);
                Console.WriteLine("Bot " + me.Username + " started...");

                // Start listening for messages
                _botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandlePollingErrorAsync,
                    receiverOptions,
                    cts.Token
                );

                Console.WriteLine("Press Enter to stop the bot.");
                Console.ReadLine();

                // Stop the bot
                cts.Cancel();
            }
        }

        private static async Task RandomNumberCommand(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            // Retrieve the active mode for the chat (defaults to "number" if not found)
            _chatModes.TryGetValue(chatId, out string currentMode);

            switch (currentMode)
            {
                case "char":
                    // Generate a random character from 'a' to 'z'
                    string alphabet = "abcdefghijklmnopqrstuvwxyz";
                    char randomChar = alphabet[CryptoRandom.Next(alphabet.Length)];

                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"🎲 Your random character (a-z): *{randomChar}*",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    break;

                case "coinflip":
                    // Reuse your coinflip logic inside the generator
                    await CoinflipCommand(bot, chatId, cancellationToken);
                    break;

                case "number":
                default:
                    // Default behavior: Generate a random number between 1 and 100
                    int randomNumber = CryptoRandom.Next(101);

                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"🎲 Your random number (0 to 100): *{randomNumber}*",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }
        private static PasswordConfig GetPasswordConfig(long chatId)
        {
            return _passwordConfigs.GetOrAdd(chatId, new PasswordConfig());
        }

        private static string GeneratePassword(PasswordConfig config)
        {
            var charPool = new StringBuilder(CharsSets.Lowercase); 
            var requiredChars = new List<char>
            {
                CharsSets.Lowercase[CryptoRandom.Next(CharsSets.Lowercase.Length)]
            };

            if (config._includeUppercase)
            {
                charPool.Append(CharsSets.Uppercase);
                requiredChars.Add(CharsSets.Uppercase[CryptoRandom.Next(CharsSets.Uppercase.Length)]);
            }

            if (config._includeNumbers)
            {
                charPool.Append(CharsSets.Numbers);
                requiredChars.Add(CharsSets.Numbers[CryptoRandom.Next(CharsSets.Numbers.Length)]);
            }

            if (config._includeSymbols)
            {
                charPool.Append(CharsSets.Symbols);
                requiredChars.Add(CharsSets.Symbols[CryptoRandom.Next(CharsSets.Symbols.Length)]);
            }

            string pool = charPool.ToString();
            int remainingLength = config._length - requiredChars.Count;

            for (int i = 0; i < remainingLength; i++)
            {
                requiredChars.Add(pool[CryptoRandom.Next(pool.Length)]);
            }

            // Fisher-Yates Shuffle
            for (int i = requiredChars.Count - 1; i > 0; i--)
            {
                int j = CryptoRandom.Next(i + 1);
                (requiredChars[i], requiredChars[j]) = (requiredChars[j], requiredChars[i]);
            }

            return new string(requiredChars.ToArray());
        }
        
        private static async Task DynamicRollCommand(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
        {
            // Split command by space (e.g., "/roll 10 50" -> ["/roll", "10", "50"])
            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 3 &&
                int.TryParse(parts[1], out int min) &&
                int.TryParse(parts[2], out int max))
            {
                // Auto-fix range bounds if user writes e.g. /roll 100 1
                if (min > max)
                {
                    (min, max) = (max, min);
                }

                int result = CryptoRandom.Next(min, max + 1);

                await bot.SendMessage(
                    chatId: chatId,
                    text: $"🎲 Random number ({min} to {max}): *{result}*",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Usage format: `/roll <min> <max>`\n*Example:* `/roll 1 100`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
        }

        private static async Task ShowGitHubLinkCommand(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            string link = "https://github.com/MaksymKozak999?tab=repositories";
            await bot.SendMessage(
                chatId: chatId,
                text: link,
                cancellationToken: cancellationToken);
        }

        private static async Task HandleUnknownOrStateMessage(ITelegramBotClient bot, long chatId, CancellationToken cancellation)
        {
            await bot.SendMessage(
                chatId: chatId,
                text: "I don't know this command, choose a button from the menu.",
                cancellationToken: cancellation
            );
        }

        private static async Task CoinflipCommand(ITelegramBotClient bot, long chatId, CancellationToken cancellation)
        {
            int coinResult = CryptoRandom.Next(2);
            string resultText = coinResult == 0 ? "*Heads*" : "*Tails*";

            await bot.SendMessage(
                chatId: chatId,
                text: resultText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellation
            );
        }

        private static async Task GenerateAndSendPassword(ITelegramBotClient bot, long chatId, CancellationToken cancellation)
        {
            var config = GetPasswordConfig(chatId);
            string password = GeneratePassword(config);

            await bot.SendMessage(
                chatId: chatId,
                text: $"🔑 *Generated Password:*\n`{password}`\n\n_Tap to copy!_",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellation
            );
        }

        private static async Task PromptForUrlCommand(ITelegramBotClient bot, long chatId, CancellationToken cancellation)
        {
            // Ставим флаг, что от этого чата мы ждем ссылку
            _awaitingUrlInput[chatId] = true;

            await bot.SendMessage(
                chatId: chatId,
                text: "🔗 **Send me a link that's too huge or not pretty, and i will make it look better**.\n\n*Or you can just type the command :* `/url https://example.com`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellation
            );
        }

        private static async Task ProcessUrlShorten(ITelegramBotClient bot, long chatId, string inputUrl, CancellationToken cancellation)
        {
            // Снимаем флаг ожидания
            _awaitingUrlInput.TryRemove(chatId, out _);

            inputUrl = inputUrl.Trim();

            // Проверка формата URL
            if (!Uri.IsWellFormedUriString(inputUrl, UriKind.Absolute))
            {
                inputUrl = "https://" + inputUrl;
                if (!Uri.IsWellFormedUriString(inputUrl, UriKind.Absolute))
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "❌ **Invalid link format.** Please give me the correct link.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellation
                    );
                    return;
                }
            }

            try
            {
                var formData = new Dictionary<string, string> { { "url", inputUrl } };

                using (var content = new FormUrlEncodedContent(formData))
                {
                    HttpResponseMessage response = await _httpClient.PostAsync("https://cleanuri.com/api/v1/shorten", content, cancellation);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        string key = "\"result_url\":\"";
                        int startIndex = responseBody.IndexOf(key);

                        if (startIndex != -1)
                        {
                            startIndex += key.Length;
                            int endIndex = responseBody.IndexOf("\"", startIndex);
                            string shortUrl = responseBody.Substring(startIndex, endIndex - startIndex).Replace("\\/", "/");

                            await bot.SendMessage(
                                chatId: chatId,
                                text: $"🔗 **Here's your lovely link:**\n{shortUrl}",
                                parseMode: ParseMode.Markdown,
                                cancellationToken: cancellation
                            );
                            return;
                        }
                    }

                    await bot.SendMessage(
                        chatId: chatId,
                        text: "❌ Sorry, i can't shorten this link.Something wrong with the server.",
                        cancellationToken: cancellation
                    );
                }
            }
            catch (Exception)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "❌ Sorry, the server is unavailable.Please try later again. ",
                    cancellationToken: cancellation
                );
            }
        }

        // Main update handler
        private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    var message = update.Message;
                    if (message.Text == null) return;

                    long chatId = message.Chat.Id;
                    string text = message.Text.Trim();

                    // System commands handler
                    if (text.StartsWith("/start"))
                    {
                        _awaitingUrlInput.TryRemove(chatId, out _);
                        await SendWelcomeMessage(bot, chatId, cancellationToken);
                        await SendMainMenu(bot, chatId, cancellationToken);
                        return;
                    }

                    // Check for dynamic custom range command: e.g. /roll 5 50
                    if (text.StartsWith("/roll"))
                    {
                        await DynamicRollCommand(bot, chatId, text, cancellationToken);
                        return;
                    }
                    if (_awaitingUrlInput.TryGetValue(chatId, out bool isAwaiting) && isAwaiting)
                    {
                       
                        if (text.StartsWith("⚙️") || text.StartsWith("🎲") || text.StartsWith("🪙") || text == "My GitHub")
                        {
                            _awaitingUrlInput.TryRemove(chatId, out _);
                        }
                        else
                        {
                            await ProcessUrlShorten(bot, chatId, text, cancellationToken);
                            return;
                        }
                    }
                    if (text.StartsWith("/url"))
                    {
                        string[] parts = text.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2)
                        {
                            await PromptForUrlCommand(bot, chatId, cancellationToken);
                        }
                        else
                        {
                            await ProcessUrlShorten(bot, chatId, parts[1], cancellationToken);
                        }
                        return;
                    }

                    // Reply keyboard buttons handler
                    switch (text)
                    {
                        case "⚙️Setting":
                            await HandleSettingsCommand(bot, chatId, cancellationToken);
                            break;
                        case "🎲Random Number":
                        case "🎲Random Character":
                            await RandomNumberCommand(bot, chatId, cancellationToken);
                            break;
                        case "🪙Coinflip":
                            await CoinflipCommand(bot, chatId, cancellationToken);
                            break;
                        case "🔗URL Shortener":
                            await PromptForUrlCommand(bot, chatId, cancellationToken);
                            break;
                        case "My GitHub":
                            await ShowGitHubLinkCommand(bot, chatId, cancellationToken);
                            break;
                        case "Password Generator":
                            await GenerateAndSendPassword(bot, chatId, cancellationToken);
                            break;
                        default:
                            await HandleUnknownOrStateMessage(bot, chatId, cancellationToken);
                            break;
                    }
                    return;
                }

                if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(bot, update.CallbackQuery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Update error: " + ex.Message);
            }
        }

        // Send menu with buttons
        private static async Task SendWelcomeMessage(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("☕ Greeting", "btn_hello"),
                    InlineKeyboardButton.WithCallbackData("ⓘ What the bot can do?", "btn_about")
                },
                new[]
                {
                    InlineKeyboardButton.WithUrl("🌐 Open website", "https://github.com/MaksymKozak999?tab=repositories")
                }
            });

            await bot.SendMessage(
                chatId: chatId,
                text: "Welcome! Choose an action below:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken
            );
        }

        private static async Task SendMainMenu(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            _chatModes.TryGetValue(chatId, out string currentMode);

            string dynamicButtonText;
            switch (currentMode)
            {
                case "char":
                    dynamicButtonText = "🎲Random Character";
                    break;
                case "coinflip":
                    dynamicButtonText = "🪙Coinflip";
                    break;
                default:
                    dynamicButtonText = "🎲Random Number";
                    break;
            }

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new [] { new KeyboardButton(dynamicButtonText), new KeyboardButton("🔗URL Shortener") },
                new [] { new KeyboardButton("📝Profile"),          new KeyboardButton("⚙️Setting") },
                new [] { new KeyboardButton("Help"), new KeyboardButton("Password Generator") },     
                new [] { new KeyboardButton("My GitHub") }
            })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

            await bot.SendMessage(
                chatId: chatId,
                text: "Main menu is open. Use the buttons below.",
                parseMode: ParseMode.Markdown,
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
        }

        private static async Task HandleSettingsCommand(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎲Change random🎲", "btn_change_random"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Password Settings", "btn_change_password_settings"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ⓘ What the bot can do?", "btn_about"),
                }

            });

            await bot.SendMessage(
               chatId: chatId,
               text: "Settings menu:",
               replyMarkup: inlineKeyboard,
               cancellationToken: cancellationToken
           );
        }

        private static async Task RandomSettingHandle(ITelegramBotClient bot, long chatId, CancellationToken cancellation)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Change to numbers", "btn_change_random_to_numbers"),
                    InlineKeyboardButton.WithCallbackData("Change to alphabet(a-z)", "btn_change_random_handling_alphabet")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Custom numbers range (/roll)", "btn_set_random_range"),
                    InlineKeyboardButton.WithCallbackData("🪙Coinflip", "btn_change_random_to_coinflip")
                },
            });

            await bot.SendMessage(
                chatId: chatId,
                text: "Random settings options:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellation
            );
        }

        private static async Task PasswordSettingsHandle(ITelegramBotClient bot, long chatId, int? messageId, CancellationToken cancellation)
        {
            var config = GetPasswordConfig(chatId);

            string upperStatus = config._includeUppercase ? "✅ Uppercase (A-Z)" : "❌ Uppercase (A-Z)";
            string numberStatus = config._includeNumbers ? "✅ Numbers (0-9)" : "❌ Numbers (0-9)";
            string symbolStatus = config._includeSymbols ? "✅ Symbols (!@#$)" : "❌ Symbols (!@#$)";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"📏 Length: {config._length} chars", "btn_pw_cycle_length")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(upperStatus, "btn_pw_toggle_upper")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(numberStatus, "btn_pw_toggle_num")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(symbolStatus, "btn_pw_toggle_sym")
                }
            });

            string menuText = "🔑 **Password Generator Settings**\n\nConfigure your default preferences below:";

            // Update existing message if called from inline callback, or send a new one
            if (messageId.HasValue)
            {
                await bot.EditMessageText(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: menuText,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellation
                );
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: menuText,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellation
                );
            }
        }

        // Button click handler
        private static async Task HandleCallbackQuery(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message == null) return;

            long chatId = callbackQuery.Message.Chat.Id;
            string action = callbackQuery.Data;
            int messageId = callbackQuery.Message.MessageId;

            await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            var config = GetPasswordConfig(chatId);

            switch (action)
            {
                case "btn_change_random":
                    await RandomSettingHandle(bot, chatId, cancellationToken);
                    break;

                case "btn_change_random_handling_alphabet":
                    _chatModes[chatId] = "char";

                    await bot.SendMessage(
                        chatId: chatId,
                        text: "✅ Mode changed! The bot will now generate random **characters (a-z)** when you use the generator.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    await SendMainMenu(bot, chatId, cancellationToken);
                    break;

                case "btn_change_random_to_numbers":
                    _chatModes[chatId] = "number";

                    await bot.SendMessage(
                        chatId: chatId,
                        text: "✅ Mode changed back to **Numbers**.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    await SendMainMenu(bot, chatId, cancellationToken);
                    break;

                case "btn_set_random_range":
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "💡 To pick a custom range at any time, simply type:\n`/roll <min> <max>`\n\n*Example:* `/roll 10 50`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    break;

                case "btn_change_random_to_coinflip":
                    _chatModes[chatId] = "coinflip";

                    await bot.SendMessage(
                       chatId: chatId,
                       text: "✅ Random mode changed to **🪙Coinflip**.",
                       parseMode: ParseMode.Markdown,
                       cancellationToken: cancellationToken
                    );

                    await SendMainMenu(bot, chatId, cancellationToken);
                    break;

                case "btn_about":
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "A personal C# project built to improve my programming skills and explore bot development. Created in my spare time for learning and personal use.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    break;
                case "btn_change_password_settings":
                    await PasswordSettingsHandle(bot, chatId, null, cancellationToken);
                    break;

                case "btn_pw_cycle_length":
                    // Cycle switch
                    switch (config._length)
                    {
                        case 8:
                            config._length = 12;
                            break;
                        case 12:
                            config._length = 16;
                            break;
                        case 16:
                            config._length = 24;
                            break;
                        case 24:
                            config._length = 32;
                            break;
                        default:
                            config._length = 8;
                            break;
                    }
                    await PasswordSettingsHandle(bot, chatId, messageId, cancellationToken);
                    break;

                case "btn_pw_toggle_upper":
                    config._includeUppercase = !config._includeUppercase;
                    await PasswordSettingsHandle(bot, chatId, messageId, cancellationToken);
                    break;

                case "btn_pw_toggle_num":
                    config._includeNumbers = !config._includeNumbers;
                    await PasswordSettingsHandle(bot, chatId, messageId, cancellationToken);
                    break;

                case "btn_pw_toggle_sym":
                    config._includeSymbols = !config._includeSymbols;
                    await PasswordSettingsHandle(bot, chatId, messageId, cancellationToken);
                    break;
   
                default:
                    Console.WriteLine($"Received unknown callback action: {action}");
                    break;
            }
        }

        // Network/API error handler
        private static Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            string errorMessage;

            if (exception is ApiRequestException apiRequestException)
            {
                errorMessage = "Telegram API error: [" + apiRequestException.ErrorCode + "]\n" + apiRequestException.Message;
            }
            else
            {
                errorMessage = exception.ToString();
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An error occurred: " + errorMessage);
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}