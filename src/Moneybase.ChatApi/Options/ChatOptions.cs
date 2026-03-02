namespace Moneybase.ChatApi.Options;

public sealed class ChatOptions
{
    public TimeOnly OfficeHoursStart { get; set; } = new(8, 0);
    public TimeOnly OfficeHoursEnd { get; set; } = new(20, 0);
    public int MaxConcurrentPerAgent { get; set; } = 10;
    public double QueueMultiplier { get; set; } = 1.5;
    public int PollIntervalSeconds { get; set; } = 1;
    public int MaxMissedPolls { get; set; } = 3;
}

public sealed class TeamScheduleOptions
{
    public List<TeamSchedule> Teams { get; set; } = new();
}

public sealed class TeamSchedule
{
    public string TeamName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
