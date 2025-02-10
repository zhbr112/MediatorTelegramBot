using Amazon.S3.Model;
using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

public class AddMediatorCommand(MediatorDbContext db, S3Client s3Client, AdminsAddMediators AdminsAdd, ILogger<AddMediatorCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return ((context.Argument?.Equals("Добавить медиатора", StringComparison.OrdinalIgnoreCase) ?? false)
            || AdminsAdd.Admins.Contains(context.Message.From.Id)) && db.Admins.Any(x => x.TelegramId == context.Message.Chat.Id);
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        var process = await db.ProcessAddMediators.FirstOrDefaultAsync(x => x.AdminId == context.Message.From.Id);

        if (process is null)
        {
            process = new ProcessAddMediator() { AdminId = context.Message.From.Id };
            AdminsAdd.Admins.Add(context.Message.From.Id);
        }
        switch (process.Step)
        {
            case 0:
                await botClient.SendMessage(context.Message.Chat.Id, "Введите имя:", cancellationToken: cancellationToken);
                process.Step++;
                await db.ProcessAddMediators.AddAsync(process);               
                break;
            case 1:
                process.Name = context.Message.Text;
                process.Step++;
                await botClient.SendMessage(context.Message.Chat.Id, "Введите описание:", cancellationToken: cancellationToken);
                break;
            case 2:
                process.Description = context.Message.Text;
                process.Step++;
                await botClient.SendMessage(context.Message.Chat.Id, "Введите телефон:", cancellationToken: cancellationToken);
                break;
            case 3:
                process.Phone = context.Message.Text;
                process.Step++;
                await botClient.SendMessage(context.Message.Chat.Id, "Введите направления через запятую:", cancellationToken: cancellationToken);
                break;
            case 4:
                process.Tags = context.Message.Text.Split([',']).Select(x => x.Trim()).ToArray();
                process.Step++;                               

                await botClient.SendMessage(context.Message.Chat.Id, "Отправьте фотографию", cancellationToken: cancellationToken);
                
                break;
            case 5:                
                var photo = context.Message.Photo[^1];

                var file = await botClient.GetFile(photo.FileId, cancellationToken);

                var fs = new FileStream(process.Id.ToString(), FileMode.Create);
                await botClient.DownloadFile(file.FilePath, fs, cancellationToken);

                var objectRequest = new PutObjectRequest()
                {
                    BucketName = "mediators",
                    Key = $"{process.Id}.jpg",
                    InputStream = fs
                };
                var reponse = await s3Client.S3.PutObjectAsync(objectRequest);

                Mediator mediator = new Mediator(process.Id, process.Name, process.Description, process.Phone, process.Tags);
                await db.Mediators.AddAsync(mediator);

                db.ProcessAddMediators.Remove(process);
                AdminsAdd.Admins.Remove(context.Message.From.Id);
                await botClient.SendMessage(context.Message.Chat.Id, "Медиатор добавлен", cancellationToken: cancellationToken);
                break;
        }
        await db.SaveChangesAsync();
    }
}