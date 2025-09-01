using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = MediatorTelegramBot.Models.User;

namespace MediatorTelegramBot.Commands;

public class AskQuestionCommand(UsersAskingQuestion usersAsking, MediatorDbContext db) : IChatCommand
{
    public bool CanExecute(CommandContext context) => context.Argument == "❓ Задать вопрос";

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken ct)
    {
        var user = db.Users.FirstOrDefault(x => x.TelegramId == context.Message.From.Id);

        if (user is null)
        {
            user = new User() { TelegramId = context.Message.From.Id, FirstName = context.Message.From.FirstName, Username = context.Message.From.Username };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        usersAsking.Users.Add(context.Message.From.Id);
        await botClient.SendMessage(context.Message.Chat.Id, "Пожалуйста, введите ваш вопрос. Мы передадим его администраторам.", cancellationToken: ct);
    }
}
