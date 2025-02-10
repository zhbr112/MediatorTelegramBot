namespace MediatorTelegramBot.Models;

public class ProcessAddMediator
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public long AdminId { get; set; }
    public int Step { get; set; } = 0;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string[] Tags { get; set; } = [];
}

