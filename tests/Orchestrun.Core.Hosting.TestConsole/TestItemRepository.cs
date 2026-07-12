using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Orchestrun.Core.Hosting.Database;
using Polly;

namespace Orchestrun.Core.Hosting.TestConsole;

/// <summary>
/// Sample repository demonstrating <see cref="OrchestrunRepositoryBase"/> usage against the
/// "OrchestrunDb" database registered via <c>AddDatabase("OrchestrunDb", ...)</c>.
/// </summary>
public sealed class TestItemRepository(
    [FromKeyedServices("OrchestrunDb")] NpgsqlDataSource dataSource,
    [FromKeyedServices("OrchestrunDb")] ResiliencePipeline pipeline,
    ILogger<TestItemRepository> logger)
    : OrchestrunRepositoryBase(dataSource, pipeline, logger)
{
    public Task AddAsync(string message, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (connection, ct) =>
        {
            await using var command = new NpgsqlCommand(
                "INSERT INTO test_items (message) VALUES (@message)", connection);
            command.Parameters.AddWithValue("message", message);
            await command.ExecuteNonQueryAsync(ct);
            return true;
        }, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(async (connection, ct) =>
        {
            await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM test_items", connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        }, cancellationToken);
}
