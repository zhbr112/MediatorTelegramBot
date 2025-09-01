using MediatorTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MediatorTelegramBot.Callback;

public class AnswerQuestionCallback(AdminsReplyingToQuestion adminsReplying) : ICallbackQuery
{
    public bool CanExecute(CallbackQuery cb) => cb.Data.StartsWith("AnswerQ ");

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery cb, CancellationToken ct)
    {
        var questionId = Guid.Parse(cb.Data.Split(' ')[1]);
        var adminId = cb.From.Id;

        adminsReplying.Admins[adminId] = questionId;

        await botClient.AnswerCallbackQuery(cb.Id, "Теперь отправьте ответ на этот вопрос мне в личные сообщения.", showAlert: true, cancellationToken: ct);
        await botClient.SendMessage(cb.From.Id, $"{cb.Message.Text}\n\nНапишите ответ на вопрос:");

        // Обновляем сообщение в чате админов, чтобы показать, кто отвечает
        await botClient.EditMessageText(cb.Message.Chat.Id, cb.Message.MessageId,
            $"{cb.Message.Text}\n\n⏳ Отвечает @{cb.From.Username}...",
            parseMode: ParseMode.Markdown, cancellationToken: ct);
    }
}
