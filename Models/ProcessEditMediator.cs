using System.ComponentModel.DataAnnotations;

namespace MediatorTelegramBot.Models;

public class ProcessEditMediator
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public long AdminId { get; set; }
    public Guid MediatorId { get; set; } // ID медиатора, которого мы редактируем
    public int Step { get; set; } = 0;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string[]? Tags { get; set; }
}