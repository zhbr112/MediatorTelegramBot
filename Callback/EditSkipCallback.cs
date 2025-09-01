using MediatorTelegramBot.Data;
using MediatorTelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediatorTelegramBot.Callback;

public class EditSkipCallback(MediatorDbContext db, EditMediatorFlowService flowService) : ICallbackQuery
{
    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.StartsWith("EditSkip ", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        var adminId = callbackQuery.From.Id;
        var process = await db.ProcessEditMediators.FirstOrDefaultAsync(p => p.AdminId == adminId, cancellationToken);

        if (process == null) return; // Процесс уже завершен или отменен

        // Удаляем сообщение с кнопкой, чтобы избежать повторных нажатий
        await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.Id, cancellationToken);

        process.Step++; // Переходим к следующему шагу
        await db.SaveChangesAsync(cancellationToken);

        // Запускаем следующий шаг
        await flowService.SendNextStepAsync(botClient, process, cancellationToken);
    }
}
