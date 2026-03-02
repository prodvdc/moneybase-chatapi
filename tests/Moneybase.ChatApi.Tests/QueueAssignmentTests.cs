using Microsoft.Extensions.DependencyInjection;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;
using Moneybase.ChatApi.Services;
using Xunit;

namespace Moneybase.ChatApi.Tests;

public sealed class QueueAssignmentTests : IClassFixture<ChatApiFactory>
{
    private readonly ChatApiFactory _factory;

    public QueueAssignmentTests(ChatApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enqueue_Refuses_When_Queue_Exceeds_Max()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();

        await SeedAgentsAsync(db, new[]
        {
            new AgentSeed("Junior-1", Seniority.Junior)
        });

        // Capacity: floor(10 * 0.4) = 4; max queue = floor(4 * 1.5) = 6
        QueueResult? last = null;
        for (var i = 0; i < 7; i++)
        {
            last = await queueService.EnqueueSessionAsync();
        }

        Assert.NotNull(last);
        Assert.False(last!.Accepted);
        Assert.Equal("queue_full", last!.Reason);
    }

    [Fact]
    public async Task Assignment_Prefers_Juniors_Before_Seniors()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();
        var assignmentService = scope.ServiceProvider.GetRequiredService<AssignmentService>();

        await SeedAgentsAsync(db, new[]
        {
            new AgentSeed("Junior-1", Seniority.Junior),
            new AgentSeed("Senior-1", Seniority.Senior)
        });

        var sessionIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var result = await queueService.EnqueueSessionAsync();
            Assert.True(result.Accepted);
            sessionIds.Add(result.SessionId!.Value);
        }

        await assignmentService.AssignOnceAsync(CancellationToken.None);

        var sessions = db.ChatSessions.Where(s => sessionIds.Contains(s.Id)).ToList();
        var junior = db.Agents.Single(a => a.Name == "Junior-1");
        var senior = db.Agents.Single(a => a.Name == "Senior-1");

        var juniorAssigned = sessions.Count(s => s.AssignedAgentId == junior.Id);
        var seniorAssigned = sessions.Count(s => s.AssignedAgentId == senior.Id);

        Assert.Equal(4, juniorAssigned);
        Assert.Equal(1, seniorAssigned);
    }

    private static async Task SeedAgentsAsync(AppDbContext db, IEnumerable<AgentSeed> agents)
    {
        db.ChatEvents.RemoveRange(db.ChatEvents);
        db.ChatSessions.RemoveRange(db.ChatSessions);
        db.Agents.RemoveRange(db.Agents);
        db.Teams.RemoveRange(db.Teams);
        db.Shifts.RemoveRange(db.Shifts);
        await db.SaveChangesAsync();

        var shift = new Shift
        {
            Name = "AllDay",
            StartTime = new TimeOnly(0, 0),
            EndTime = new TimeOnly(23, 59)
        };

        var team = new Team
        {
            Name = "Team A",
            IsOverflow = false,
            Shift = shift
        };

        db.Shifts.Add(shift);
        db.Teams.Add(team);

        foreach (var agentSeed in agents)
        {
            db.Agents.Add(new Agent
            {
                Name = agentSeed.Name,
                Seniority = agentSeed.Seniority,
                Team = team
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed record AgentSeed(string Name, Seniority Seniority);
}
