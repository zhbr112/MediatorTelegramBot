namespace MediatorTelegramBot.Models;

public class Mediator
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Phone { get; set; }

    public string[] Tags { get; set; }

    // Навигационные свойства
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public Mediator()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        Description = string.Empty;
        Phone = string.Empty;
        Tags = [];
    }
    public Mediator(Guid id, string name, string description, string phone, string[] tags)
    {
        Id = id;
        Name = name;
        Description = description;
        Phone = phone;
        Tags = tags;
    }
}
