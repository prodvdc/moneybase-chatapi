namespace Moneybase.ChatApi.Models;

public enum ChatSessionStatus
{
    Queued = 0,
    Active = 1,
    Inactive = 2,
    Closed = 3,
    Refused = 4
}

public sealed class ChatSession
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChatSessionStatus Status { get; set; }
    public DateTimeOffset? LastPolledAt { get; set; }
    public int MissedPolls { get; set; }
    public int? AssignedAgentId { get; set; }
    public Agent? AssignedAgent { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public List<ChatEvent> Events { get; set; } = new();
}
