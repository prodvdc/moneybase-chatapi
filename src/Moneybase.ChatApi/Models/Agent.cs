namespace Moneybase.ChatApi.Models;

public sealed class Agent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Seniority Seniority { get; set; }
    public bool IsActive { get; set; } = true;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public DateTimeOffset? LastAssignedAt { get; set; }
    public List<ChatSession> ChatSessions { get; set; } = new();
}
