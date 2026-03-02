using Microsoft.EntityFrameworkCore;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;

namespace Moneybase.ChatApi.Services;

public sealed class AssignmentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AssignmentService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AssignOnceAsync(stoppingToken);
            }
            catch
            {
                // Swallow exceptions to keep the worker alive; logging can be added later.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public async Task AssignOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var capacityService = scope.ServiceProvider.GetRequiredService<CapacityService>();

        var now = DateTimeOffset.Now;
        var baseCapacity = await capacityService.GetTeamCapacityAsync(now, includeOverflow: false);
        var baseMaxQueue = capacityService.GetMaxQueueLength(baseCapacity);
        var queuedCount = await db.ChatSessions.CountAsync(c => c.Status == ChatSessionStatus.Queued, cancellationToken);

        var includeOverflow = capacityService.IsOfficeHours(now) && queuedCount >= baseMaxQueue;
        var agents = await capacityService.GetAssignableAgentsAsync(now, includeOverflow);
        if (agents.Count == 0)
        {
            return;
        }

        var queuedSessions = await db.ChatSessions
            .Where(c => c.Status == ChatSessionStatus.Queued)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        if (queuedSessions.Count == 0)
        {
            return;
        }

        var activeCounts = await db.ChatSessions
            .Where(c => c.Status == ChatSessionStatus.Active && c.AssignedAgentId != null)
            .GroupBy(c => c.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var activeLookup = activeCounts.ToDictionary(x => x.AgentId, x => x.Count);

        var orderedAgents = agents
            .OrderBy(a => GetSeniorityPriority(a.Seniority))
            .ThenBy(a => a.LastAssignedAt ?? DateTimeOffset.MinValue)
            .ThenBy(a => a.Id)
            .ToList();

        var available = new Dictionary<int, int>();
        foreach (var agent in orderedAgents)
        {
            var capacity = capacityService.GetAgentCapacity(agent);
            var active = activeLookup.TryGetValue(agent.Id, out var count) ? count : 0;
            var remaining = Math.Max(0, capacity - active);
            if (remaining > 0)
            {
                available[agent.Id] = remaining;
            }
        }

        if (available.Count == 0)
        {
            return;
        }

        var sessionIndex = 0;
        var groups = orderedAgents.GroupBy(a => GetSeniorityPriority(a.Seniority)).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var groupAgents = group.ToList();
            var agentIndex = 0;
            var groupAvailable = groupAgents
                .Where(a => available.ContainsKey(a.Id))
                .ToDictionary(a => a.Id, a => available[a.Id]);

            var maxIterations = queuedSessions.Count * groupAgents.Count;
            var iterations = 0;

            while (sessionIndex < queuedSessions.Count && groupAvailable.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var agent = groupAgents[agentIndex % groupAgents.Count];
                agentIndex++;

                if (!groupAvailable.TryGetValue(agent.Id, out var remaining) || remaining <= 0)
                {
                    continue;
                }

                var session = queuedSessions[sessionIndex];
                session.AssignedAgentId = agent.Id;
                session.AssignedAt = now;
                session.Status = ChatSessionStatus.Active;
                agent.LastAssignedAt = now;
                db.ChatEvents.Add(new ChatEvent
                {
                    SessionId = session.Id,
                    Type = ChatEventType.Assigned,
                    OccurredAt = now,
                    Metadata = $"agentId={agent.Id};seniority={agent.Seniority}"
                });

                remaining--;
                if (remaining == 0)
                {
                    groupAvailable.Remove(agent.Id);
                    available.Remove(agent.Id);
                }
                else
                {
                    groupAvailable[agent.Id] = remaining;
                    available[agent.Id] = remaining;
                }

                sessionIndex++;
            }

            if (sessionIndex >= queuedSessions.Count)
            {
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static int GetSeniorityPriority(Seniority seniority)
    {
        return seniority switch
        {
            Seniority.Junior => 0,
            Seniority.MidLevel => 1,
            Seniority.Senior => 2,
            Seniority.TeamLead => 3,
            _ => 0
        };
    }
}
