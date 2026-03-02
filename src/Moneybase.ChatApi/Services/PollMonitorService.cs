using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;
using Moneybase.ChatApi.Options;

namespace Moneybase.ChatApi.Services;

public sealed class PollMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PollMonitorService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAsync(stoppingToken);
            }
            catch
            {
                // Swallow exceptions to keep the worker alive; logging can be added later.
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<ChatOptions>>().Value;

        var now = DateTimeOffset.Now;
        var interval = TimeSpan.FromSeconds(options.PollIntervalSeconds);

        var sessions = await db.ChatSessions
            .Where(c => c.Status == ChatSessionStatus.Queued || c.Status == ChatSessionStatus.Active)
            .ToListAsync(cancellationToken);

        var changed = false;

        foreach (var session in sessions)
        {
            var lastActivity = session.LastPolledAt ?? session.CreatedAt;
            var missed = (int)Math.Floor((now - lastActivity).TotalSeconds / interval.TotalSeconds);
            if (missed < options.MaxMissedPolls)
            {
                if (session.MissedPolls != missed)
                {
                    session.MissedPolls = missed;
                    changed = true;
                }
                continue;
            }

            session.MissedPolls = missed;
            session.Status = ChatSessionStatus.Inactive;
            db.ChatEvents.Add(new ChatEvent
            {
                SessionId = session.Id,
                Type = ChatEventType.Inactivated,
                OccurredAt = now,
                Metadata = $\"missed={missed}\"
            });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
