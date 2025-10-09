using Amazon.S3.Model;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Services;

public class MediatorCardService
{
    private readonly MediatorDbContext _db;
    private readonly S3Client _s3Client;

    public MediatorCardService(MediatorDbContext db, S3Client s3Client)
    {
        _db = db;
        _s3Client = s3Client;
    }

    /// <summary>
    /// ОТПРАВЛЯЕТ НОВОЕ сообщение с карточкой медиатора.
    /// </summary>
    public async Task SendMediatorCardAsync(ITelegramBotClient botClient, long chatId, Guid mediatorId, long userId, CancellationToken cancellationToken)
    {
        var cardData = await PrepareMediatorCardDataAsync(mediatorId, userId, cancellationToken);
        if (cardData == null)
        {
            await botClient.SendMessage(chatId, "К сожалению, этот медиатор больше не доступен.", cancellationToken: cancellationToken);
            return;
        }

        if (cardData.Value.photoUrl != null)
        {
            await botClient.SendPhoto(chatId, cardData.Value.photoUrl,
                caption: cardData.Value.content, replyMarkup: cardData.Value.keyboard, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId, cardData.Value.content,
                replyMarkup: cardData.Value.keyboard, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// РЕДАКТИРУЕТ СУЩЕСТВУЮЩЕЕ сообщение, превращая его в карточку медиатора.
    /// </summary>
    public async Task EditMessageToMediatorCardAsync(ITelegramBotClient botClient, Message messageToEdit, Guid mediatorId, long userId, CancellationToken cancellationToken)
    {
        var cardData = await PrepareMediatorCardDataAsync(mediatorId, userId, cancellationToken);
        if (cardData == null)
        {
            // Если медиатор удален, просто сообщим об этом
            await botClient.EditMessageText(messageToEdit.Chat.Id, messageToEdit.MessageId,
                "К сожалению, этот медиатор больше не доступен.", cancellationToken: cancellationToken);
            return;
        }

        // Важный момент: мы не можем отредактировать текстовое сообщение, добавив в него фото.
        // Telegram API этого не позволяет. Поэтому мы просто отредактируем текст/подпись.
        // Наш универсальный хелпер EditMessageContentAsync идеально для этого подходит.
        await botClient.EditMessageContentAsync(
            messageToEdit,
            cardData.Value.content,
            cardData.Value.keyboard,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Приватный метод для подготовки данных карточки, чтобы избежать дублирования кода.
    /// </summary>
    private async Task<(string content, InlineKeyboardMarkup keyboard, string? photoUrl)?> PrepareMediatorCardDataAsync(Guid mediatorId, long userId, CancellationToken cancellationToken)
    {
        var mediator = await _db.Mediators
            .Include(m => m.Reviews)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mediatorId, cancellationToken);

        if (mediator == null) return null;

        var user = await _db.Users
            .Include(u => u.FavoriteMediators)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TelegramId == userId, cancellationToken);

        var reviews = mediator.Reviews;
        var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        var ratingString = averageRating > 0 ? $"⭐ {averageRating:F1}/5.0 ({reviews.Count} отзывов)" : "⭐ Нет отзывов";
        var tags = string.Join("\n", mediator.Tags.Select(x => $"\t• {x}"));

        var content = $"{mediator.Name}\n{ratingString}\n\n{mediator.Description}\n\nтел.{mediator.Phone}";

        var isFavorite = user?.FavoriteMediators.Any(fm => fm.Id == mediator.Id) ?? false;
        var favoriteButtonText = isFavorite ? "❤️ Убрать из избранного" : "🤍 Добавить в избранное";

        var keyboard = new InlineKeyboardMarkup(
        [
            [InlineKeyboardButton.WithCallbackData("✍️ Оставить отзыв", $"AddReview {mediator.Id}")],
            [InlineKeyboardButton.WithCallbackData("👀 Посмотреть отзывы", $"ViewReviews {mediator.Id} 0")],
            [InlineKeyboardButton.WithCallbackData(favoriteButtonText, $"ToggleFav {mediator.Id}")]
        ]);

        var photoUrl = await GetPresignedUrlAsync(_s3Client, mediator.Id.ToString());

        return (content, keyboard, photoUrl);
    }

    // Вспомогательный метод для S3
    private async Task<string?> GetPresignedUrlAsync(S3Client s3Client, string key)
    {
        var listResponse = await s3Client.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "mediators",
            Prefix = $"{key}.jpg"
        });

        if (listResponse.S3Objects is null)
        {
            return null;
        }

        var presignRequest = new GetPreSignedUrlRequest()
        {
            BucketName = "mediators",
            Key = $"{key}.jpg",
            Expires = DateTime.UtcNow.AddMinutes(5),
        };
        return s3Client.S3.GetPreSignedURL(presignRequest);
    }
}
