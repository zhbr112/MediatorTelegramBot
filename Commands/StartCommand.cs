using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

[BotCommand("/start", "Sends a greeting message")]
public class StartCommand(MediatorDbContext db, ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Command?.Equals("/start", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Sending greeting message...");

        List<KeyboardButton[]> keyboardButtons = [];

        keyboardButtons.Add([new KeyboardButton("Найти медиатора")]);

        if (db.Admins.Any(x => x.TelegramId == context.Message.Chat.Id))
            keyboardButtons.AddRange([[new KeyboardButton("Добавить медиатора")], 
            [new KeyboardButton("Удалить медиатора")]]);

        ReplyKeyboardMarkup replyKeyboardMarkup = new(keyboardButtons){ ResizeKeyboard = true };

        await botClient.SendMessage(context.Message.Chat.Id, CommandStrings.Start_Welcome,
            replyMarkup:replyKeyboardMarkup, cancellationToken: cancellationToken);
    }
}