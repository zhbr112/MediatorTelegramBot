using MediatorTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediatorTelegramBot.Callback;

public class GoBackToMediatorCallback : ICallbackQuery
{
    private readonly MediatorCardService _cardService;

    public GoBackToMediatorCallback(MediatorCardService cardService)
    {
        _cardService = cardService;
    }

    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("GoBackToMediator ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var dataParts = callbackQuery.Data!.Split(' ');
        if (dataParts.Length < 2 || !Guid.TryParse(dataParts[1], out var mediatorId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка: неверный ID", cancellationToken: cancellationToken);
            return;
        }

        // Отвечаем на коллбэк, чтобы убрать "часики"
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        // Просто вызываем новый метод для редактирования
        await _cardService.EditMessageToMediatorCardAsync(
            botClient,
            callbackQuery.Message, // Передаем все сообщение для редактирования
            mediatorId,
            callbackQuery.From.Id, // Передаем ID пользователя для проверки "избранного"
            cancellationToken);
    }
}
