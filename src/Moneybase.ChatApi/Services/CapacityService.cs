using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;
using Moneybase.ChatApi.Options;

namespace Moneybase.ChatApi.Services;

public sealed class CapacityService
{
    private readonly AppDbContext _db;
    private readonly ChatOptions _options;

    public CapacityService(AppDbContext db, IOptions<ChatOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public bool IsOfficeHours(DateTimeOffset now)
    {
        return IsWithinWindow(TimeOnly.FromDateTime(now.LocalDateTime), _options.OfficeHoursStart, _options.OfficeHoursEnd);
    }

    public async Task<IReadOnlyList<Agent>> GetAssignableAgentsAsync(DateTimeOffset now, bool includeOverflow)
    {
        var nowTime = TimeOnly.FromDateTime(now.LocalDateTime);

        var query = _db.Agents
            .Include(a => a.Team)
            .ThenInclude(t => t.Shift)
            .Where(a => a.IsActive);

        if (!includeOverflow)
        {
            query = query.Where(a => !a.Team.IsOverflow);
        }

        var agents = await query.ToListAsync();
        return agents
            .Where(a => IsWithinWindow(nowTime, a.Team.Shift.StartTime, a.Team.Shift.EndTime))
            .ToList();
    }

    public int GetAgentCapacity(Agent agent)
    {
        var multiplier = GetSeniorityMultiplier(agent.Seniority);
        return (int)Math.Floor(_options.MaxConcurrentPerAgent * multiplier);
    }

    public double GetSeniorityMultiplier(Seniority seniority)
    {
        return seniority switch
        {
            Seniority.Junior => 0.4,
            Seniority.MidLevel => 0.6,
            Seniority.Senior => 0.8,
            Seniority.TeamLead => 0.5,
            _ => 0.4
        };
    }

    public async Task<int> GetTeamCapacityAsync(DateTimeOffset now, bool includeOverflow)
    {
        var agents = await GetAssignableAgentsAsync(now, includeOverflow);
        return agents.Sum(GetAgentCapacity);
    }

    public int GetMaxQueueLength(int capacity)
    {
        return (int)Math.Floor(capacity * _options.QueueMultiplier);
    }

    private static bool IsWithinWindow(TimeOnly now, TimeOnly start, TimeOnly end)
    {
        if (start == end)
        {
            return true;
        }

        if (start < end)
        {
            return now >= start && now < end;
        }

        return now >= start || now < end;
    }
}
