using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using MediatorTelegramBot.Resources;
using Telegram.Bot.Types.ReplyMarkups;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Commands;
using Microsoft.Extensions.Logging;
using MediatorTelegramBot.Models;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;

namespace MediatorTelegramBot.Callback;

public class DeleteMediatorsCallback(MediatorDbContext db, S3Client s3Client) : ICallbackQuery
{
    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.Split(' ')[0].Equals("DeleteMed", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var id = new Guid(callbackQuery.Data![(callbackQuery.Data!.IndexOf(' ') + 1)..]);

        await botClient.AnswerCallbackQuery(callbackQuery.Id);

        await db.Mediators.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);

        await s3Client.S3.DeleteObjectAsync("mediators", $"{id}.jpg");

        await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.Id);

        db.SaveChanges();
    }
}
