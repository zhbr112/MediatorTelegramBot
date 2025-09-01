using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

public class EditMediatorsCommand(MediatorDbContext db) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return (context.Argument?.Equals("Редактировать медиатора", StringComparison.OrdinalIgnoreCase) ?? false)
               && db.Admins.Any(a => a.TelegramId == context.Message.Chat.Id);
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var mediators = await db.Mediators.ToListAsync(cancellationToken);

        if (!mediators.Any())
        {
            await botClient.SendMessage(context.Message.Chat.Id, "Список медиаторов пуст.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(context.Message.Chat.Id, "Выберите медиатора для редактирования:", cancellationToken: cancellationToken);

        foreach (var mediator in mediators)
        {
            var message = $"{mediator.Name}\n{mediator.Description}";
            var button = InlineKeyboardButton.WithCallbackData("Редактировать", $"EditMed {mediator.Id}");
            var keyboard = new InlineKeyboardMarkup(button);

            await botClient.SendMessage(
                chatId: context.Message.Chat.Id,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }
}
