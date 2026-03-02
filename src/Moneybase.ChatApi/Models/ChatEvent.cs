namespace Moneybase.ChatApi.Models;

public enum ChatEventType
{
    Created = 0,
    Assigned = 1,
    Polled = 2,
    Inactivated = 3,
    Closed = 4
}

public sealed class ChatEvent
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;
    public ChatEventType Type { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Metadata { get; set; }
}
