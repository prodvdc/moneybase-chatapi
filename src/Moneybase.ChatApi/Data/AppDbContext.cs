using Microsoft.EntityFrameworkCore;
using Moneybase.ChatApi.Models;

namespace Moneybase.ChatApi.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatEvent> ChatEvents => Set<ChatEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shift>()
            .HasMany(s => s.Teams)
            .WithOne(t => t.Shift)
            .HasForeignKey(t => t.ShiftId);

        modelBuilder.Entity<Team>()
            .HasMany(t => t.Agents)
            .WithOne(a => a.Team)
            .HasForeignKey(a => a.TeamId);

        modelBuilder.Entity<Agent>()
            .HasMany(a => a.ChatSessions)
            .WithOne(c => c.AssignedAgent)
            .HasForeignKey(c => c.AssignedAgentId);

        modelBuilder.Entity<ChatSession>()
            .HasMany(c => c.Events)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId);

        modelBuilder.Entity<ChatSession>()
            .Property(c => c.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Agent>()
            .Property(a => a.Seniority)
            .HasConversion<int>();

        modelBuilder.Entity<ChatEvent>()
            .Property(e => e.Type)
            .HasConversion<int>();

        modelBuilder.Entity<Shift>()
            .Property(s => s.StartTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));

        modelBuilder.Entity<Shift>()
            .Property(s => s.EndTime)
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v));
    }
}
