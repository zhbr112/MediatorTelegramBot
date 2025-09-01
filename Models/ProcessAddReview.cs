namespace MediatorTelegramBot.Models;

public class ProcessAddReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public Guid MediatorId { get; set; }
    public int Step { get; set; } = 0;
    public int? Rating { get; set; }
}
