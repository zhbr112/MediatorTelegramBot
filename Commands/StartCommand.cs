using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

[BotCommand("/start", "Sends a greeting message")]
public class StartCommand(ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Command?.Equals("/start", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Sending greeting message...");

        ReplyKeyboardMarkup replyKeyboardMarkup = new(new KeyboardButton("Найти медиатора"))
        { ResizeKeyboard = true };

        await botClient.SendMessage(context.Message.Chat.Id, CommandStrings.Start_Welcome,
            replyMarkup:replyKeyboardMarkup, cancellationToken: cancellationToken);
    }
}