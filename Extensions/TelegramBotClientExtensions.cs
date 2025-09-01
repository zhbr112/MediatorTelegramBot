using Amazon.S3.Model;
using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Models;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = MediatorTelegramBot.Models.User;

namespace MediatorTelegramBot.Extensions;

public static class TelegramBotClientExtensions
{
    /// <summary>
    /// Универсально редактирует сообщение, определяя, нужно ли менять Caption или Text.
    /// </summary>
    public static async Task EditMessageContentAsync(
        this ITelegramBotClient botClient,
        Message messageToEdit,
        string newText,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Markdown,
        CancellationToken cancellationToken = default)
    {
        if (messageToEdit.Caption != null)
        {
            // Если у сообщения есть Caption, значит это медиа (фото/видео)
            await botClient.EditMessageCaption(
                chatId: messageToEdit.Chat.Id,
                messageId: messageToEdit.MessageId,
                caption: newText,
                parseMode: parseMode,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Иначе это обычное текстовое сообщение
            await botClient.EditMessageText(
                chatId: messageToEdit.Chat.Id,
                messageId: messageToEdit.MessageId,
                text: newText,
                parseMode: parseMode,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
    }
    public static async Task WriteMediatorAsync(this ITelegramBotClient botClient, Mediator mediator, User user, S3Client s3Client, long chatId, CancellationToken cancellationToken)
    {
        // Расчет рейтинга
        var reviews = mediator.Reviews;
        var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        var ratingString = averageRating > 0 ? $"⭐ {averageRating:F1}/5.0 ({reviews.Count} отзывов)" : "⭐ Нет отзывов";

        // Формируем основное сообщение
        var message = $"{mediator.Name}\n{ratingString}\n\n{mediator.Description}\n\nтел.{mediator.Phone}";

        // Проверяем, в избранном ли этот медиатор у пользователя
        var isFavorite = user?.FavoriteMediators.Any(fm => fm.Id == mediator.Id) ?? false;
        var favoriteButtonText = isFavorite ? "❤️ Убрать из избранного" : "🤍 Добавить в избранное";

        // Создаем клавиатуру
        var keyboard = new InlineKeyboardMarkup([

            // Первый ряд
            [
                    InlineKeyboardButton.WithCallbackData("✍️ Оставить отзыв", $"AddReview {mediator.Id}"),
                    InlineKeyboardButton.WithCallbackData("👀 Посмотреть отзывы", $"ViewReviews {mediator.Id} 0"), // 0 - номер страницы
                ],
                // Второй ряд
                [
                    InlineKeyboardButton.WithCallbackData(favoriteButtonText, $"ToggleFav {mediator.Id}"),
                ]
        ]);

        var listResponse = await s3Client.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "mediators",
            Prefix = $"{mediator.Id}.jpg"
        });

        if (listResponse.S3Objects is null)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            return;
        }

        // Отправка фото с подписью и кнопками (ваш код для S3)
        var presignRequest = new GetPreSignedUrlRequest()
        {
            BucketName = "mediators",
            Key = $"{mediator.Id}.jpg",
            Expires = DateTime.UtcNow.AddSeconds(10),
        };

        var presignedUrlResponse = s3Client.S3.GetPreSignedURL(presignRequest);
        await botClient.SendPhoto(
            chatId: chatId,
            photo: presignedUrlResponse,
            caption: message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    //private async Task<string?> GetPresignedUrlAsync(S3Client s3Client, string key)
    //{
    //    try
    //    {
    //        var presignRequest = new GetPreSignedUrlRequest()
    //        {
    //            BucketName = "mediators",
    //            Key = $"{key}.jpg",
    //            Expires = DateTime.UtcNow.AddMinutes(5), // Дадим больше времени на загрузку
    //        };
    //        return s3Client.S3.GetPreSignedURL(presignRequest);
    //    }
    //    catch (Exception) // Ловим возможные ошибки, если объекта нет
    //    {
    //        return null;
    //    }
    //}
}
