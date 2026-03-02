using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moneybase.ChatApi.Models;
using Moneybase.ChatApi.Options;

namespace Moneybase.ChatApi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration)
    {
        if (await db.Teams.AnyAsync())
        {
            return;
        }

        var chatOptions = configuration.GetSection("ChatOptions").Get<ChatOptions>() ?? new ChatOptions();
        var scheduleOptions = configuration.GetSection("TeamSchedules").Get<TeamScheduleOptions>() ?? new TeamScheduleOptions();

        var shifts = new Dictionary<string, Shift>(StringComparer.OrdinalIgnoreCase);
        foreach (var teamSchedule in scheduleOptions.Teams)
        {
            if (!shifts.ContainsKey(teamSchedule.TeamName))
            {
                shifts[teamSchedule.TeamName] = new Shift
                {
                    Name = $"{teamSchedule.TeamName} Shift",
                    StartTime = teamSchedule.StartTime,
                    EndTime = teamSchedule.EndTime
                };
            }
        }

        if (!shifts.ContainsKey("Overflow"))
        {
            shifts["Overflow"] = new Shift
            {
                Name = "Overflow Shift",
                StartTime = chatOptions.OfficeHoursStart,
                EndTime = chatOptions.OfficeHoursEnd
            };
        }

        db.Shifts.AddRange(shifts.Values);
        await db.SaveChangesAsync();

        var teamA = new Team { Name = "Team A", IsOverflow = false, ShiftId = shifts["Team A"].Id };
        var teamB = new Team { Name = "Team B", IsOverflow = false, ShiftId = shifts["Team B"].Id };
        var teamC = new Team { Name = "Team C", IsOverflow = false, ShiftId = shifts["Team C"].Id };
        var overflow = new Team { Name = "Overflow", IsOverflow = true, ShiftId = shifts["Overflow"].Id };

        db.Teams.AddRange(teamA, teamB, teamC, overflow);
        await db.SaveChangesAsync();

        var agents = new List<Agent>
        {
            new Agent { Name = "A-Lead", Seniority = Seniority.TeamLead, TeamId = teamA.Id },
            new Agent { Name = "A-Mid-1", Seniority = Seniority.MidLevel, TeamId = teamA.Id },
            new Agent { Name = "A-Mid-2", Seniority = Seniority.MidLevel, TeamId = teamA.Id },
            new Agent { Name = "A-Junior", Seniority = Seniority.Junior, TeamId = teamA.Id },

            new Agent { Name = "B-Senior", Seniority = Seniority.Senior, TeamId = teamB.Id },
            new Agent { Name = "B-Mid", Seniority = Seniority.MidLevel, TeamId = teamB.Id },
            new Agent { Name = "B-Junior-1", Seniority = Seniority.Junior, TeamId = teamB.Id },
            new Agent { Name = "B-Junior-2", Seniority = Seniority.Junior, TeamId = teamB.Id },

            new Agent { Name = "C-Mid-1", Seniority = Seniority.MidLevel, TeamId = teamC.Id },
            new Agent { Name = "C-Mid-2", Seniority = Seniority.MidLevel, TeamId = teamC.Id },

            new Agent { Name = "Overflow-1", Seniority = Seniority.Junior, TeamId = overflow.Id },
            new Agent { Name = "Overflow-2", Seniority = Seniority.Junior, TeamId = overflow.Id },
            new Agent { Name = "Overflow-3", Seniority = Seniority.Junior, TeamId = overflow.Id },
            new Agent { Name = "Overflow-4", Seniority = Seniority.Junior, TeamId = overflow.Id },
            new Agent { Name = "Overflow-5", Seniority = Seniority.Junior, TeamId = overflow.Id },
            new Agent { Name = "Overflow-6", Seniority = Seniority.Junior, TeamId = overflow.Id }
        };

        db.Agents.AddRange(agents);
        await db.SaveChangesAsync();
    }
}
