using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = MediatorTelegramBot.Models.User;

namespace MediatorTelegramBot.Callback;

public class AddReviewCallback : ICallbackQuery
{
    private readonly MediatorDbContext _db;
    private readonly UsersAddingReview _usersAdding;

    public AddReviewCallback(MediatorDbContext db, UsersAddingReview usersAdding)
    {
        _db = db;
        _usersAdding = usersAdding;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("AddReview ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var dataParts = callbackQuery.Data!.Split(' ');
        if (dataParts.Length < 2) return;

        var userId = callbackQuery.From.Id;
        var mediatorId = new Guid(dataParts[1]);

        var user = _db.Users.FirstOrDefault(x => x.TelegramId == userId);

        if (user is null)
        {
            user = new User() { TelegramId = callbackQuery.From.Id, FirstName = callbackQuery.From.FirstName, Username = callbackQuery.From.Username };
            _db.Users.Add(user);
        }

        // Проверка, не оставлял ли пользователь уже отзыв
        var existingReview = await _db.Reviews.AnyAsync(r => r.MediatorId == mediatorId && r.UserId == userId, cancellationToken);
        if (existingReview)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Вы уже оставляли отзыв этому медиатору.", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        // Начинаем процесс
        var process = new ProcessAddReview { UserId = userId, MediatorId = mediatorId, Step = 1 };
        _db.ProcessAddReviews.Add(process);
        _usersAdding.Users.Add(userId);
        await _db.SaveChangesAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        var ratingKeyboard = new InlineKeyboardMarkup([
        
            [InlineKeyboardButton.WithCallbackData("⭐", $"SetRating {1}")],
            [InlineKeyboardButton.WithCallbackData("⭐⭐", $"SetRating {2}")],
            [InlineKeyboardButton.WithCallbackData("⭐⭐⭐", $"SetRating {3}")],
            [InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐", $"SetRating {4}")],
            [InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐", $"SetRating {5}")],
        ]);

        // Заменяем карточку медиатора на предложение оценить
        await botClient.EditMessageContentAsync(
            messageToEdit: callbackQuery.Message, // Передаем все сообщение
            newText: "Пожалуйста, оцените специалиста:", // Текст для замены
            replyMarkup: ratingKeyboard,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }
}
