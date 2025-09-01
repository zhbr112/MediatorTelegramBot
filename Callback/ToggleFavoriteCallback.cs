using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = MediatorTelegramBot.Models.User;

namespace MediatorTelegramBot.Callback;

public class ToggleFavoriteCallback : ICallbackQuery
{
    private readonly MediatorDbContext _db;

    public ToggleFavoriteCallback(MediatorDbContext db)
    {
        _db = db;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("ToggleFav ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var dataParts = callbackQuery.Data!.Split(' ');
        if (dataParts.Length < 2 || !Guid.TryParse(dataParts[1], out var mediatorId)) return;

        var userId = callbackQuery.From.Id;

        var user = await _db.Users
            .Include(u => u.FavoriteMediators)
            .FirstOrDefaultAsync(u => u.TelegramId == userId, cancellationToken);

        // Если пользователя нет в БД, создаем его
        if (user == null)
        {
            user = new User { TelegramId = userId, FirstName = callbackQuery.From.FirstName, Username = callbackQuery.From.Username };
            _db.Users.Add(user);
        }

        var mediator = await _db.Mediators.FindAsync(new object[] { mediatorId }, cancellationToken);
        if (mediator == null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Медиатор не найден", cancellationToken: cancellationToken);
            return;
        }

        var isFavorite = user.FavoriteMediators.Any(fm => fm.Id == mediatorId);
        string alertText;
        string newButtonText;

        if (isFavorite)
        {
            user.FavoriteMediators.Remove(mediator);
            alertText = "Удалено из избранного";
            newButtonText = "🤍 Добавить в избранное";
        }
        else
        {
            user.FavoriteMediators.Add(mediator);
            alertText = "Добавлено в избранное";
            newButtonText = "❤️ Убрать из избранного";
        }

        await _db.SaveChangesAsync(cancellationToken);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, alertText, cancellationToken: cancellationToken);

        // Обновляем кнопку в сообщении, чтобы пользователь видел результат
        var originalMarkup = callbackQuery.Message.ReplyMarkup;
        var firstRow = originalMarkup.InlineKeyboard.First();
        var newMarkup = new InlineKeyboardMarkup(new[]
        {
            firstRow, // Оставляем первый ряд кнопок (отзывы) как есть
            new[] { InlineKeyboardButton.WithCallbackData(newButtonText, $"ToggleFav {mediatorId}") }
        });

        await botClient.EditMessageReplyMarkup(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: newMarkup,
            cancellationToken: cancellationToken);
    }
}
