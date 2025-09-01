namespace MediatorTelegramBot.Models;

public class AdminsReplyingToQuestion { public Dictionary<long, Guid> Admins { get; } = new(); } // Key: AdminId, Value: QuestionId
