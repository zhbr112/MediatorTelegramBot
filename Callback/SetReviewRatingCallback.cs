using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Callback;

public class SetReviewRatingCallback : ICallbackQuery
{
    private readonly MediatorDbContext _db;
    private readonly UsersAddingReview _usersAdding;

    public SetReviewRatingCallback(MediatorDbContext db, UsersAddingReview usersAdding)
    {
        _db = db;
        _usersAdding = usersAdding;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("SetRating ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var dataParts = callbackQuery.Data!.Split(' ');
        if (dataParts.Length < 2 || !int.TryParse(dataParts[1], out var rating)) return;

        var userId = callbackQuery.From.Id;
        var process = await _db.ProcessAddReviews.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (process == null) return;

        process.Rating = rating;
        process.Step = 2; // Переходим к шагу ввода текста
        await _db.SaveChangesAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        // Заменяем сообщение с оценкой на предложение ввести текст
        await botClient.EditMessageContentAsync(
            messageToEdit: callbackQuery.Message, // Передаем все сообщение
            newText: "Спасибо за оценку! Теперь вы можете написать текстовый отзыв или завершить, нажав кнопку ниже.", // Текст для замены
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("✅ Завершить без текста", "FinishReview")),
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }
}
