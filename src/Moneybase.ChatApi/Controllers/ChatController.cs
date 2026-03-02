using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Models;
using Moneybase.ChatApi.Services;

namespace Moneybase.ChatApi.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly QueueService _queueService;

    public ChatController(AppDbContext db, QueueService queueService)
    {
        _db = db;
        _queueService = queueService;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession(CancellationToken cancellationToken)
    {
        var result = await _queueService.EnqueueSessionAsync(cancellationToken);
        if (!result.Accepted)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                accepted = false,
                reason = result.Reason
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            accepted = true,
            sessionId = result.SessionId
        });
    }

    [HttpPost("sessions/{id:guid}/poll")]
    public async Task<IActionResult> Poll(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (session == null)
        {
            return NotFound();
        }

        session.LastPolledAt = DateTimeOffset.Now;
        session.MissedPolls = 0;
        _db.ChatEvents.Add(new ChatEvent
        {
            SessionId = session.Id,
            Type = ChatEventType.Polled,
            OccurredAt = session.LastPolledAt.Value
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            assignedAgentId = session.AssignedAgentId,
            assignedAgentSeniority = session.AssignedAgent?.Seniority.ToString()
        });
    }

    [HttpPost("sessions/{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (session == null)
        {
            return NotFound();
        }

        if (session.Status != ChatSessionStatus.Closed)
        {
            session.Status = ChatSessionStatus.Closed;
            _db.ChatEvents.Add(new ChatEvent
            {
                SessionId = session.Id,
                Type = ChatEventType.Closed,
                OccurredAt = DateTimeOffset.Now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status.ToString()
        });
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.ChatSessions
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (session == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            assignedAgentId = session.AssignedAgentId,
            assignedAgentSeniority = session.AssignedAgent?.Seniority.ToString(),
            createdAt = session.CreatedAt,
            lastPolledAt = session.LastPolledAt,
            missedPolls = session.MissedPolls
        });
    }

    [HttpGet("sessions/{id:guid}/events")]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken cancellationToken)
    {
        var events = await _db.ChatEvents
            .Where(e => e.SessionId == id)
            .OrderBy(e => e.OccurredAt)
            .Select(e => new
            {
                type = e.Type.ToString(),
                occurredAt = e.OccurredAt,
                metadata = e.Metadata
            })
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            var exists = await _db.ChatSessions.AnyAsync(s => s.Id == id, cancellationToken);
            if (!exists)
            {
                return NotFound();
            }
        }

        return Ok(events);
    }
}
