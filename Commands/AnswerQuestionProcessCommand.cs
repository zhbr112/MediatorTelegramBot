using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MediatorTelegramBot.Commands;

public class AnswerQuestionProcessCommand(
    MediatorDbContext db,
    AdminsReplyingToQuestion adminsReplying,
    IOptions<BotConfiguration> config) : IChatCommand
{
    private readonly long _adminChatId = config.Value.AdminChatId;

    public bool CanExecute(CommandContext context) => adminsReplying.Admins.ContainsKey(context.Message.From.Id);

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken ct)
    {
        var adminId = context.Message.From.Id;
        var adminUsername = context.Message.From.Username;
        var answerText = context.Message.Text;

        if (!adminsReplying.Admins.TryGetValue(adminId, out var questionId)) return;

        adminsReplying.Admins.Remove(adminId);

        var question = await db.Questions.FindAsync(questionId);
        if (question == null || question.Status == QuestionStatus.Answered)
        {
            await botClient.SendMessage(adminId, "Этот вопрос уже был отвечен или удален.", cancellationToken: ct);
            return;
        }

        // Обновляем вопрос в БД
        question.AnswerText = answerText;
        question.AnswererAdminId = adminId;
        question.AnsweredAt = DateTime.UtcNow;
        question.Status = QuestionStatus.Answered;
        await db.SaveChangesAsync(ct);

        // Уведомляем пользователя
        var notificationText = $"🔔 Администратор ответил на ваш вопрос!\n\n" +
                               $"*Ваш вопрос:*\n_{question.QuestionText}_\n\n" +
                               $"*Ответ:*\n_{answerText}_";
        try
        {
            await botClient.SendMessage(question.AskerId, notificationText, parseMode: ParseMode.Markdown, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Пользователь мог заблокировать бота, сообщаем об этом админу
            await botClient.SendMessage(adminId, $"Не удалось отправить ответ пользователю {question.AskerId}. Возможно, он заблокировал бота.", cancellationToken: ct);
        }

        // Уведомляем админа об успехе
        await botClient.SendMessage(adminId, "✅ Ваш ответ успешно отправлен пользователю.", cancellationToken: ct);

        // Обновляем сообщение в чате админов на финальное
        var originalQuestionText = (await botClient.GetChat(_adminChatId, ct)).Description; // Это хак, надо текст хранить
        var newAdminText = $"❓ Вопрос от пользователя (ID: `{question.AskerId}`):\n\n_{question.QuestionText}_\n\n" +
                           $"✅ Ответил @{adminUsername}:\n\n_{answerText}_";

        await botClient.EditMessageText(_adminChatId, question.AdminChatMessageId, newAdminText, ParseMode.Markdown, cancellationToken: ct);
    }
}
