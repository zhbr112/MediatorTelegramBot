using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Callback;

public class ViewReviewsCallback : ICallbackQuery
{
    private readonly MediatorDbContext _db;
    private const int PageSize = 1; // По 1 отзыва на странице

    public ViewReviewsCallback(MediatorDbContext db)
    {
        _db = db;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("ViewReviews ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var parts = callbackQuery.Data!.Split(' ');
        if (parts.Length < 3 || !Guid.TryParse(parts[1], out var mediatorId) || !int.TryParse(parts[2], out var page))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка данных", cancellationToken: cancellationToken);
            return;
        }

        var reviewsQuery = _db.Reviews.Where(r => r.MediatorId == mediatorId).OrderByDescending(r => r.CreatedAt);
        var totalReviews = await reviewsQuery.CountAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        if (totalReviews == 0)
        {
            await botClient.EditMessageContentAsync(
            messageToEdit: callbackQuery.Message, // Передаем все сообщение
            newText: "У этого медиатора пока нет отзывов.", // Текст для замены
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("« Назад", $"GoBackToMediator {mediatorId}")),
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
            );
            return;
        }

        var reviewsOnPage = await reviewsQuery.Skip(page * PageSize).Take(PageSize).Include(u => u.User).ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"Отзывы ({page + 1} из {(totalReviews + PageSize - 1) / PageSize}):\n");
        foreach (var review in reviewsOnPage)
        {
            var ratingStars = new string('⭐', review.Rating) + new string('☆', 5 - review.Rating);
            sb.AppendLine($"@{review.User.Username} {ratingStars}");
            if (!string.IsNullOrWhiteSpace(review.Text))
            {
                sb.AppendLine($"{review.Text}");
            }
            sb.AppendLine($"_{review.CreatedAt:dd.MM.yyyy}_");
            sb.AppendLine("----------");
        }

        // Кнопки пагинации
        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0)
        {
            navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Пред.", $"ViewReviews {mediatorId} {page - 1}"));
        }
        if ((page + 1) * PageSize < totalReviews)
        {
            navButtons.Add(InlineKeyboardButton.WithCallbackData("След. ➡️", $"ViewReviews {mediatorId} {page + 1}"));
        }

        var keyboardRows = new List<IEnumerable<InlineKeyboardButton>> { navButtons };
        keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData("« Назад к медиатору", $"GoBackToMediator {mediatorId}") });

        await botClient.EditMessageContentAsync(
            messageToEdit: callbackQuery.Message, // Передаем все сообщение
            newText: sb.ToString(), // Текст для замены
            replyMarkup: new InlineKeyboardMarkup(keyboardRows),
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }
}
