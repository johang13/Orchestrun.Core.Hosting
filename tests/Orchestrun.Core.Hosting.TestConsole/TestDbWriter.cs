using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Orchestrun.Core.Hosting.TestConsole;

/// <summary>
/// Demonstrates the database wiring end-to-end: writes one row and logs the resulting row
/// count on startup, proving that migrations, the keyed <see cref="Npgsql.NpgsqlDataSource"/>/
/// <see cref="Polly.ResiliencePipeline"/>, and repository resolution all work together.
/// </summary>
public sealed class TestDbWriter(TestItemRepository repository, ILogger<TestDbWriter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await repository.AddAsync($"Hello DB! - {DateTime.Now:O}", stoppingToken);
        var count = await repository.CountAsync(stoppingToken);
        logger.LogInformation("test_items now has {Count} row(s)", count);
    }
}
