using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediatorTelegramBot.Models;

public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Range(1, 5)]
    public int Rating { get; set; } // Оценка от 1 до 5
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Внешний ключ для Медиатора
    public Guid MediatorId { get; set; }
    [ForeignKey("MediatorId")]
    public virtual Mediator Mediator { get; set; }

    // Внешний ключ для Пользователя
    public long UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
