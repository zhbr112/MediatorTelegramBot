using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediatorTelegramBot.Callbackж
{
    public class EditMediatorCallback(MediatorDbContext db, AdminsEditMediators adminsEdit, EditMediatorFlowService flowService) : ICallbackQuery
    {
        public bool CanExecute(CallbackQuery callbackQuery)
        {
            return callbackQuery.Data?.StartsWith("EditMed ", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            var adminId = callbackQuery.From.Id;
            var dataParts = callbackQuery.Data!.Split(' ');
            if (dataParts.Length < 2 || !Guid.TryParse(dataParts[1], out var mediatorId)) return;

            // Начинаем процесс редактирования
            var process = new ProcessEditMediator
            {
                AdminId = adminId,
                MediatorId = mediatorId,
                Step = 1 // Начинаем с первого шага
            };

            await db.ProcessEditMediators.AddAsync(process, cancellationToken);
            adminsEdit.Admins.Add(adminId);
            await db.SaveChangesAsync(cancellationToken);

            // Удаляем сообщение с выбором медиатора для чистоты
            await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.Id, cancellationToken);

            // Запускаем первый шаг через наш новый сервис
            await flowService.SendNextStepAsync(botClient, process, cancellationToken);
        }
    }
}
