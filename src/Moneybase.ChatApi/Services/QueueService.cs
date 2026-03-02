using Microsoft.EntityFrameworkCore;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;

namespace Moneybase.ChatApi.Services;

public sealed class QueueService
{
    private readonly AppDbContext _db;
    private readonly CapacityService _capacityService;

    public QueueService(AppDbContext db, CapacityService capacityService)
    {
        _db = db;
        _capacityService = capacityService;
    }

    public async Task<QueueResult> EnqueueSessionAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var includeOverflow = false;
        var baseCapacity = await _capacityService.GetTeamCapacityAsync(now, includeOverflow: false);
        var baseMaxQueue = _capacityService.GetMaxQueueLength(baseCapacity);
        var queueLength = await _db.ChatSessions.CountAsync(c => c.Status == ChatSessionStatus.Queued, cancellationToken);

        if (queueLength >= baseMaxQueue)
        {
            if (_capacityService.IsOfficeHours(now))
            {
                includeOverflow = true;
                var overflowCapacity = await _capacityService.GetTeamCapacityAsync(now, includeOverflow: true);
                var overflowMaxQueue = _capacityService.GetMaxQueueLength(overflowCapacity);
                if (queueLength >= overflowMaxQueue)
                {
                    return QueueResult.Refused("queue_full");
                }
            }
            else
            {
                return QueueResult.Refused("queue_full");
            }
        }

        if (baseCapacity == 0 && !includeOverflow)
        {
            return QueueResult.Refused("no_agents_available");
        }

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            Status = ChatSessionStatus.Queued,
            LastPolledAt = null,
            MissedPolls = 0
        };

        _db.ChatSessions.Add(session);
        _db.ChatEvents.Add(new ChatEvent
        {
            SessionId = session.Id,
            Type = ChatEventType.Created,
            OccurredAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        return QueueResult.Accepted(session.Id);
    }
}

public sealed record QueueResult(bool Accepted, Guid? SessionId, string? Reason)
{
    public static QueueResult Accepted(Guid sessionId) => new(true, sessionId, null);
    public static QueueResult Refused(string reason) => new(false, null, reason);
}
