using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediatorTelegramBot.Models;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string QuestionText { get; set; }
    public string? AnswerText { get; set; }

    public QuestionStatus Status { get; set; } = QuestionStatus.Pending;

    public DateTime AskedAt{get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }

    // Связь с пользователем, который задал вопрос
    public long AskerId { get; set; }
    [ForeignKey("AskerId")]
    public virtual User Asker { get; set; }

    // ID администратора, который ответил
    public long? AnswererAdminId { get; set; }

    // ID сообщения в чате админов, чтобы мы могли его редактировать
    public int AdminChatMessageId { get; set; }
}

public enum QuestionStatus
{
    Pending, // В ожидании
    Answered // Отвечен
}