using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;


namespace MediatorTelegramBot.Commands;

public class DeleteMediatorsCommand(MediatorDbContext db, ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Argument?.Equals("Удалить медиатора", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        var mediators = db.Mediators.ToArray();

        foreach (var mediator in mediators)
        {
            var message = $"{mediator.Name}\n{mediator.Description}\n{string.Join("\n", mediator.Tags.Select(x => $"- {x}"))}\nтел.{mediator.Phone}";

            await botClient.SendMessage(context.Message.Chat.Id, message,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Удалить", $"DeleteMed {mediator.Id}")),
            cancellationToken: cancellationToken);
        }
    }
}