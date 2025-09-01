using Amazon.S3.Model;
using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Extensions;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

public class MyFavoritesCommand(MediatorDbContext db, MediatorCardService cardService) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Argument?.Equals("❤️ Мои избранные", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From.Id;

        var user = await db.Users
            .Include(u => u.FavoriteMediators)
                .ThenInclude(m => m.Reviews) // Включаем отзывы для расчета рейтинга
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TelegramId == userId, cancellationToken);

        if (user == null || !user.FavoriteMediators.Any())
        {
            await botClient.SendMessage(context.Message.Chat.Id, "Ваш список избранного пуст.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(context.Message.Chat.Id, "Ваши избранные медиаторы:", cancellationToken: cancellationToken);

        foreach (var mediator in user.FavoriteMediators)
        {
            await cardService.SendMediatorCardAsync(botClient, context.Message.Chat.Id, mediator.Id, userId, cancellationToken);
        }
    }
}
