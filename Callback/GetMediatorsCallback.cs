using Amazon.S3.Model;
using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using MediatorTelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Callback;

public class GetMediatorsCallback(MediatorDbContext db, MediatorCardService cardService) : ICallbackQuery
{
    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.Split(' ')[0].Equals("Mediators", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var tag = callbackQuery.Data![(callbackQuery.Data!.IndexOf(' ') + 1)..];
        var userId = callbackQuery.From.Id;

        // Получаем медиаторов, включая их отзывы для расчета рейтинга
        var mediators = await db.Mediators
            .Include(m => m.Reviews)
            .Where(x => x.Tags.Contains(tag))
            .ToListAsync(cancellationToken);

        // Получаем пользователя и его избранных, чтобы знать, какую кнопку "Избранное" показывать
        var user = await db.Users
            .Include(u => u.FavoriteMediators)
            .FirstOrDefaultAsync(u => u.TelegramId == userId, cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        if (!mediators.Any())
        {
            await botClient.SendMessage(callbackQuery.Message.Chat.Id, "В этой категории пока нет медиаторов.", cancellationToken: cancellationToken);
            return;
        }

        foreach (var mediator in mediators)
        {
            await cardService.SendMediatorCardAsync(botClient, callbackQuery.Message.Chat.Id, mediator.Id, userId, cancellationToken);
        }
    }
}
