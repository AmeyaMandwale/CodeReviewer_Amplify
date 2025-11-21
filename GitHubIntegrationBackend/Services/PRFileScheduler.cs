using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GitHubIntegrationBackend.Data;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

public class PRFileScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30); // faster than PR sync

    public PRFileScheduler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine($"🔄 PR File Scheduler started at {DateTime.UtcNow}");

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var prFileSync = scope.ServiceProvider.GetRequiredService<PRFileSyncService>();

                // get all PRs
                var allPRs = await ctx.PullRequests
                    .Include(x => x.Repository)
                    .ToListAsync(stoppingToken);

                foreach (var pr in allPRs)
                {
                    Console.WriteLine($"🔍 Syncing PR files for PR {pr.Id}");

                    try
                    {
                        await prFileSync.SyncPRFiles(pr.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Error syncing PR {pr.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PR File Scheduler ERROR: {ex.Message}");
            }

            Console.WriteLine($"⏳ Sleeping 30 mins before next PR file sync…");
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
