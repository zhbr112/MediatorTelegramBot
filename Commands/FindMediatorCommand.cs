using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;


namespace MediatorTelegramBot.Commands;

public class FindMediatorCommand(MediatorDbContext db, ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Argument?.Equals("Наши медиаторы", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        var tags=db.Mediators.SelectMany(m => m.Tags).Distinct().ToArray();
        List<InlineKeyboardButton[]> inlineKeyboardButtons = [];

        foreach (var tag in tags)
        {
            inlineKeyboardButtons.Add([InlineKeyboardButton.WithCallbackData(tag, $"Mediators {tag}")]);
        }

        var inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons);

        await botClient.SendMessage(context.Message.Chat.Id, "Выберите направление:",
            replyMarkup:inlineKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
}