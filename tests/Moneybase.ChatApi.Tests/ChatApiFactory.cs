using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moneybase.ChatApi.Data;
using Moneybase.ChatApi.Options;

namespace Moneybase.ChatApi.Tests;

public sealed class ChatApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ChatOptions:OfficeHoursStart"] = "00:00",
                ["ChatOptions:OfficeHoursEnd"] = "23:59",
                ["ChatOptions:MaxConcurrentPerAgent"] = "10",
                ["ChatOptions:QueueMultiplier"] = "1.5",
                ["ChatOptions:PollIntervalSeconds"] = "1",
                ["ChatOptions:MaxMissedPolls"] = "3",
                ["TeamSchedules:Teams:0:TeamName"] = "Team A",
                ["TeamSchedules:Teams:0:StartTime"] = "00:00",
                ["TeamSchedules:Teams:0:EndTime"] = "23:59",
                ["TeamSchedules:Teams:1:TeamName"] = "Team B",
                ["TeamSchedules:Teams:1:StartTime"] = "00:00",
                ["TeamSchedules:Teams:1:EndTime"] = "23:59",
                ["TeamSchedules:Teams:2:TeamName"] = "Team C",
                ["TeamSchedules:Teams:2:StartTime"] = "00:00",
                ["TeamSchedules:Teams:2:EndTime"] = "23:59",
                ["TeamSchedules:Teams:3:TeamName"] = "Overflow",
                ["TeamSchedules:Teams:3:StartTime"] = "00:00",
                ["TeamSchedules:Teams:3:EndTime"] = "23:59"
            };
            config.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase($"ChatDb_{Guid.NewGuid()}");
            });
        });
    }
}
