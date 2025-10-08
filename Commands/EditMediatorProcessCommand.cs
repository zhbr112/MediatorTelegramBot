using Amazon.S3.Model;
using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediatorTelegramBot.Commands;

public class EditMediatorProcessCommand(MediatorDbContext db, S3Client s3Client, AdminsEditMediators adminsEdit, EditMediatorFlowService flowService) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return adminsEdit.Admins.Contains(context.Message.From.Id);
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var adminId = context.Message.From.Id;
        var process = await db.ProcessEditMediators.FirstOrDefaultAsync(p => p.AdminId == adminId, cancellationToken);
        if (process == null) return;

        // Обрабатываем ввод пользователя в зависимости от текущего шага
        switch (process.Step)
        {
            case 1: process.Name = context.Message.Text; break;
            case 2: process.Description = context.Message.Text; break;
            case 3: process.Phone = context.Message.Text; break;
            case 4: process.Tags = context.Message.Text.Split(',').Select(t => t.Trim()).ToArray(); break;
            case 5:
                if (context.Message.Photo is { Length: > 0 })
                {
                    await s3Client.S3.DeleteObjectAsync("mediators", $"{process.MediatorId}.jpg", cancellationToken);

                    var photo = context.Message.Photo[^1];

                    var file = await botClient.GetFile(photo.FileId, cancellationToken);

                    var fs = new FileStream(process.Id.ToString(), FileMode.Create);
                    await botClient.DownloadFile(file.FilePath, fs, cancellationToken);

                    var objectRequest = new PutObjectRequest()
                    {
                        BucketName = "mediators",
                        Key = $"{process.MediatorId}.jpg",
                        InputStream = fs
                    };
                    var reponse = await s3Client.S3.PutObjectAsync(objectRequest, cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(context.Message.Chat.Id, "Пожалуйста, отправьте фотографию или нажмите 'Пропустить'.", cancellationToken: cancellationToken);
                    return; // Не переходим на следующий шаг, ждем фото или нажатия кнопки
                }
                break;
        }

        process.Step++; // Переходим к следующему шагу
        await db.SaveChangesAsync(cancellationToken);

        // Запускаем следующий шаг
        await flowService.SendNextStepAsync(botClient, process, cancellationToken);
    }
}