using System.ComponentModel.DataAnnotations;

namespace MediatorTelegramBot.Models;

public class User
{
    [Key]
    public long TelegramId { get; set; } // Используем Telegram ID как ключ
    public string? FirstName { get; set; }
    public string? Username { get; set; }

    // Навигационные свойства
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Mediator> FavoriteMediators { get; set; } = new List<Mediator>();
}
