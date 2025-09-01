using Amazon.Runtime.Internal.Util;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Callback;

public class FinishReviewCallback : ICallbackQuery
{
    private readonly MediatorDbContext _db;
    private readonly UsersAddingReview _usersAdding;

    public FinishReviewCallback(MediatorDbContext db, UsersAddingReview usersAdding)
    {
        _db = db;
        _usersAdding = usersAdding;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("FinishReview", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var process = await _db.ProcessAddReviews.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (process == null || process.Step != 2) return;

        // Создаем и сохраняем отзыв без текста
        var review = new Review
        {
            UserId = process.UserId,
            MediatorId = process.MediatorId,
            Rating = process.Rating.Value
        };      

        _db.Reviews.Add(review);

        // Очистка
        _db.ProcessAddReviews.Remove(process);
        _usersAdding.Users.Remove(userId);
        await _db.SaveChangesAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, "Отзыв сохранен!", cancellationToken: cancellationToken);
        // Заменяем сообщение на финальное
        await botClient.EditMessageContentAsync(
            messageToEdit: callbackQuery.Message, // Передаем все сообщение
            newText: "Спасибо, ваш отзыв принят!", // Текст для замены
            replyMarkup: null,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }
}
