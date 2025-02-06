using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

//[BotCommand("Найти медиатора", "Найти медиатора")]
public class FindMediatorCommand(ILogger<StartCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Argument?.Equals("Найти медиатора", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboardMarkup = new(InlineKeyboardButton.WithCallbackData("А это просто кнопка", "Mediators 1"));

        await botClient.SendMessage(context.Message.Chat.Id, CommandStrings.Start_Welcome,
            replyMarkup:inlineKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
}