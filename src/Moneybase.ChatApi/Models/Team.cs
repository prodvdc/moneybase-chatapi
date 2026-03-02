namespace Moneybase.ChatApi.Models;

public sealed class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOverflow { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public List<Agent> Agents { get; set; } = new();
}
