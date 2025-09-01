using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediatorTelegramBot.Commands;

public class AddReviewProcessCommand : IChatCommand
{
    private readonly MediatorDbContext _db;
    private readonly UsersAddingReview _usersAdding;

    public AddReviewProcessCommand(MediatorDbContext db, UsersAddingReview usersAdding)
    {
        _db = db;
        _usersAdding = usersAdding;
    }

    public bool CanExecute(CommandContext context)
    {
        return _usersAdding.Users.Contains(context.Message.From.Id);
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From.Id;
        var process = await _db.ProcessAddReviews.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (process == null || process.Step != 2) return;

        // Создаем и сохраняем отзыв
        var review = new Review
        {
            UserId = process.UserId,
            MediatorId = process.MediatorId,
            Rating = process.Rating.Value, // Рейтинг уже должен быть установлен
            Text = context.Message.Text
        };
        Console.WriteLine($"{review.UserId} {review.MediatorId}");
        Console.WriteLine(string.Join(" ,", _db.Users.Select(x => x.TelegramId)));
        Console.WriteLine(string.Join(" ,", _db.Mediators.Select(x => x.Id)));

        _db.Reviews.Add(review);

        // Очистка
        _db.ProcessAddReviews.Remove(process);
        _usersAdding.Users.Remove(userId);
        await _db.SaveChangesAsync(cancellationToken);

        // Уведомляем пользователя и удаляем предыдущее сообщение
        await botClient.SendMessage(context.Message.Chat.Id, "Спасибо, ваш отзыв принят!", cancellationToken: cancellationToken);
    }
}
