using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

[BotCommand("/start", "Sends a greeting message")]
public class StartCommand(MediatorDbContext db, ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Command?.Equals("/start", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var fromUser = context.Message.From;
        if (fromUser == null) return; // Защита от сообщений не от пользователя

        try
        {
            // 1. Ищем пользователя в базе данных
            var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramId == fromUser.Id, cancellationToken);

            // 2. Если пользователя нет (user == null), создаем нового
            if (user == null)
            {
                user = new User
                {
                    TelegramId = fromUser.Id,
                    FirstName = fromUser.FirstName,
                    Username = fromUser.Username
                };
                db.Users.Add(user);
                logger.LogInformation("New user registered: {UserId} ({Username})", fromUser.Id, fromUser.Username);
            }
            else // Опционально: обновляем данные, если пользователь изменил имя/юзернейм в Telegram
            {
                if (user.FirstName != fromUser.FirstName || user.Username != fromUser.Username)
                {
                    user.FirstName = fromUser.FirstName;
                    user.Username = fromUser.Username;
                    logger.LogInformation("User data updated for: {UserId}", fromUser.Id);
                }
            }

            // 3. Сохраняем изменения в БД (добавление нового или обновление существующего)
            await db.SaveChangesAsync(cancellationToken);

            // --- КОНЕЦ ДОБАВЛЕННОЙ ЛОГИКИ ---

            logger.LogDebug("Sending greeting message to user {UserId}...", fromUser.Id);

            // Ваша существующая логика для создания клавиатуры
            List<KeyboardButton[]> keyboardButtons = [];
            keyboardButtons.AddRange
            (
                [new KeyboardButton("❤️ Мои избранные")], 
                [new KeyboardButton("Наши медиаторы"), new KeyboardButton("🔍 Поиск")],
                [new KeyboardButton("❓ Задать вопрос")]
            );

            if (await db.Admins.AnyAsync(x => x.TelegramId == context.Message.Chat.Id, cancellationToken))
            {
                keyboardButtons.AddRange(
                [
                    [new KeyboardButton("Добавить медиатора")],
                    [new KeyboardButton("Редактировать медиатора")],
                    [new KeyboardButton("Удалить медиатора")]
                ]);
            }

            ReplyKeyboardMarkup replyKeyboardMarkup = new(keyboardButtons) { ResizeKeyboard = true };

            // Можно сделать приветствие более личным
            var welcomeMessage = $"Здравствуйте, {fromUser.FirstName}!\n\n{CommandStrings.Start_Welcome}\n\nМедиатор — это профессиональный посредник, который помогает сторонам конфликта найти компромисс. Его задача не в том, чтобы вынести решение, а в том, чтобы организовать переговоры и подвести участников к взаимовыгодному соглашению.";
            Console.WriteLine(context.Message.Chat.Id);
            await botClient.SendMessage(context.Message.Chat.Id, welcomeMessage,
                replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing /start command for user {UserId}", fromUser.Id);
            // Сообщаем пользователю об ошибке
            await botClient.SendMessage(context.Message.Chat.Id, "Произошла внутренняя ошибка. Пожалуйста, попробуйте позже.", cancellationToken: cancellationToken);
        }
    }
}