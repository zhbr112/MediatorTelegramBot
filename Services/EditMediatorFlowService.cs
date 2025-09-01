using Amazon.S3.Model;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Services;

public class EditMediatorFlowService(MediatorDbContext db, S3Client s3Client, AdminsEditMediators adminsEdit)
{
    // Метод для отправки сообщения следующего шага или завершения процесса
    public async Task SendNextStepAsync(ITelegramBotClient botClient, ProcessEditMediator process, CancellationToken cancellationToken)
    {
        var mediator = await db.Mediators.FindAsync(new object[] { process.MediatorId }, cancellationToken);
        if (mediator == null)
        {
            // Обработка случая, если медиатор был удален во время редактирования
            await botClient.SendMessage(process.AdminId, "Ошибка: редактируемый медиатор не найден.", cancellationToken: cancellationToken);
            CleanUpProcess(process.AdminId);
            return;
        }

        string message;
        InlineKeyboardMarkup keyboard;

        switch (process.Step)
        {
            case 1:
                message = $"Введите новое имя.\nТекущее: {mediator.Name}";
                keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));
                await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                break;
            case 2:
                message = $"Введите новое описание.\nТекущее: {mediator.Description}";
                keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));
                await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                break;
            case 3:
                message = $"Введите новый телефон.\nТекущий: {mediator.Phone}";
                keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));
                await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                break;
            case 4:
                message = $"Введите новые направления через запятую.\nТекущие: {string.Join(", ", mediator.Tags)}";
                keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));
                await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                break;

            case 5: // Получаем теги
                message = "Отправьте новую фотографию.";
                keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));

                var listResponse = await s3Client.S3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = "mediators",
                    Prefix = $"{mediator.Id}.jpg"
                });

                if (listResponse.S3Objects is null)
                {
                    await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
                    break;
                }

                var presignRequest = new GetPreSignedUrlRequest()
                {
                    BucketName = "mediators",
                    Key = $"{mediator.Id}.jpg",
                    Expires = DateTime.UtcNow.AddSeconds(10),
                };

                var presignedUrlResponse = s3Client.S3.GetPreSignedURL(presignRequest);

                await botClient.SendPhoto(process.AdminId, presignedUrlResponse, caption: message, replyMarkup: keyboard,
                    cancellationToken: cancellationToken);

                break;
            //case 5:
            //    message = "Отправьте новую фотографию.";
            //    keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Пропустить", $"EditSkip {process.Step}"));
            //    await botClient.SendMessage(process.AdminId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
            //    break;
            case 6: // Завершающий шаг
                await CompleteEditing(botClient, process, mediator, cancellationToken);
                break;
        }
    }

    private async Task CompleteEditing(ITelegramBotClient botClient, ProcessEditMediator process, Mediator mediator, CancellationToken cancellationToken)
    {
        // Применяем накопленные изменения
        if (process.Name != null) mediator.Name = process.Name;
        if (process.Description != null) mediator.Description = process.Description;
        if (process.Phone != null) mediator.Phone = process.Phone;
        if (process.Tags != null) mediator.Tags = process.Tags;
        // Фотография обрабатывается в EditMediatorProcessCommand

        db.Mediators.Update(mediator);

        await botClient.SendMessage(process.AdminId, "Данные медиатора успешно обновлены.", cancellationToken: cancellationToken);

        CleanUpProcess(process.AdminId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public void CleanUpProcess(long adminId)
    {
        // Удаляем временную запись из БД и из сервиса отслеживания
        var processToRemove = db.ProcessEditMediators.FirstOrDefault(p => p.AdminId == adminId);
        if (processToRemove != null)
        {
            db.ProcessEditMediators.Remove(processToRemove);
        }
        adminsEdit.Admins.Remove(adminId);
    }
}
