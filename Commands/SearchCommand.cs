using MediatorTelegramBot.Models;
using Telegram.Bot;

namespace MediatorTelegramBot.Commands;

public class SearchCommand : IChatCommand
{
    private readonly UsersInSearchProcess _usersInSearch;

    public SearchCommand(UsersInSearchProcess usersInSearch)
    {
        _usersInSearch = usersInSearch;
    }

    public bool CanExecute(CommandContext context)
    {
        // Убедитесь, что текст на кнопке совпадает с этой строкой
        return context.Argument?.Equals("🔍 Поиск", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From.Id;

        // 1. Добавляем пользователя в список ожидающих ввода
        _usersInSearch.Users.Add(userId);

        // 2. Просим пользователя ввести запрос
        await botClient.SendMessage(
            chatId: context.Message.Chat.Id,
            text: "Введите имя, тег, часть описания или любое ключевое слово для поиска:",
            cancellationToken: cancellationToken);
    }
}
