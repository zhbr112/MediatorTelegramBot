using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Commands;

public class AskQuestionProcessCommand(
MediatorDbContext db,
UsersAskingQuestion usersAsking,
IOptions<BotConfiguration> config) : IChatCommand
{
    private readonly long _adminChatId = config.Value.AdminChatId;

    public bool CanExecute(CommandContext context) => usersAsking.Users.Contains(context.Message.From.Id);

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken ct)
    {
        var userId = context.Message.From.Id;
        var userName = context.Message.From.Username ?? context.Message.From.FirstName;
        var questionText = context.Message.Text;

        usersAsking.Users.Remove(userId);

        var question = new Question { AskerId = userId, QuestionText = questionText };

        var messageToAdmin = $"❓ Новый вопрос от пользователя @{userName} (ID: `{userId}`):\n\n_{questionText}_";
        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("✍️ Ответить", $"AnswerQ {question.Id}"));
        var sentMessage = await botClient.SendMessage(_adminChatId, messageToAdmin, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);

        question.AdminChatMessageId = sentMessage.MessageId;

        db.Questions.Add(question);
        await db.SaveChangesAsync(ct);

        await botClient.SendMessage(userId, "✅ Спасибо! Ваш вопрос отправлен администраторам. Вы получите уведомление, как только на него ответят.", cancellationToken: ct);
    }
}
