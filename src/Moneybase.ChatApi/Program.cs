using Microsoft.EntityFrameworkCore;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Options;
using Moneybase.ChatApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<ChatOptions>(builder.Configuration.GetSection("ChatOptions"));
builder.Services.Configure<TeamScheduleOptions>(builder.Configuration.GetSection("TeamSchedules"));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChatDb");
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<CapacityService>();
builder.Services.AddScoped<QueueService>();

builder.Services.AddSingleton<AssignmentService>();
builder.Services.AddSingleton<PollMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AssignmentService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<PollMonitorService>());

var app = builder.Build();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbInitializer.SeedAsync(db, builder.Configuration);
}

app.Run();

public partial class Program { }
