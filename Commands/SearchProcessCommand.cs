using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MediatorTelegramBot.Commands;

public class SearchProcessCommand : IChatCommand
{
    private readonly MediatorDbContext _db;
    private readonly UsersInSearchProcess _usersInSearch;
    private readonly MediatorCardService _cardService;

    public SearchProcessCommand(MediatorDbContext db, UsersInSearchProcess usersInSearch, MediatorCardService cardService)
    {
        _db = db;
        _usersInSearch = usersInSearch;
        _cardService = cardService;
    }

    public bool CanExecute(CommandContext context)
    {
        // Срабатывает, если пользователь в режиме поиска и прислал обычный текст (не команду)
        return _usersInSearch.Users.Contains(context.Message.From.Id)
               && context.Message.Type == MessageType.Text
               && !context.Message.Text.StartsWith("/");
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From.Id;
        var query = context.Message.Text?.Trim();

        // 1. Сразу убираем пользователя из режима поиска, чтобы он не застрял в нем
        _usersInSearch.Users.Remove(userId);

        // 2. Валидация запроса
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            await botClient.SendMessage(context.Message.Chat.Id, "Поисковый запрос слишком короткий. Пожалуйста, введите минимум 3 символа.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(context.Message.Chat.Id, $"🔍 Идет поиск по запросу: \"{query}\"...", cancellationToken: cancellationToken);

        var lowerQuery = query.ToLower();

        // 3. Выполняем поиск по нескольким полям
        var foundMediatorIds = await _db.Mediators
            .Where(m =>
                EF.Functions.ILike(m.Name, $"%{query}%") || // Регистронезависимый поиск для PostgreSQL
                EF.Functions.ILike(m.Description, $"%{query}%") ||
                m.Tags.Any(t => EF.Functions.ILike(t, $"%{query}%"))
            )
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        // 4. Обрабатываем результаты
        if (!foundMediatorIds.Any())
        {
            await botClient.SendMessage(context.Message.Chat.Id, "По вашему запросу ничего не найдено. Попробуйте изменить ключевые слова.", cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(context.Message.Chat.Id, $"Найдено медиаторов: {foundMediatorIds.Count}", cancellationToken: cancellationToken);

        // 5. Отправляем карточки найденных медиаторов, используя наш сервис
        foreach (var mediatorId in foundMediatorIds)
        {
            await _cardService.SendMediatorCardAsync(botClient, context.Message.Chat.Id, mediatorId, userId, cancellationToken);
        }
    }
}
