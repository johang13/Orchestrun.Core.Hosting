using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Orchestrun.Core.Hosting.Database;

/// <summary>
/// Base class for Postgres repositories. Derived types should request their
/// <see cref="NpgsqlDataSource"/> and <see cref="ResiliencePipeline"/> using constructor
/// parameters attributed with <c>[FromKeyedServices("yourDatabaseName")]</c>, where the name
/// matches the one passed to <c>AddDatabase</c> during host setup.
/// </summary>
public abstract class OrchestrunRepositoryBase(
    NpgsqlDataSource dataSource,
    ResiliencePipeline pipeline,
    ILogger logger)
{
    protected NpgsqlDataSource DataSource { get; } = dataSource;
    protected ResiliencePipeline Pipeline { get; } = pipeline;
    protected ILogger Logger { get; } = logger;

    protected async Task<T> ExecuteAsync<T>(
        Func<NpgsqlConnection, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? operationName = null)
    {
        try
        {
            return await Pipeline.ExecuteAsync(async ct =>
            {
                await using var connection = await DataSource.OpenConnectionAsync(ct);
                return await operation(connection, ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database operation {Operation} failed in {Repository}.", operationName, GetType().Name);
            throw;
        }
    }

    protected async Task ExecuteInTransactionAsync(
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? operationName = null)
    {
        try
        {
            await Pipeline.ExecuteAsync(async ct =>
            {
                await using var connection = await DataSource.OpenConnectionAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(ct);
                try
                {
                    await operation(connection, transaction, ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database operation {Operation} failed in {Repository}.", operationName, GetType().Name);
            throw;
        }
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? operationName = null)
    {
        try
        {
            return await Pipeline.ExecuteAsync(async ct =>
            {
                await using var connection = await DataSource.OpenConnectionAsync(ct);
                await using var transaction = await connection.BeginTransactionAsync(ct);
                try
                {
                    var result = await operation(connection, transaction, ct);
                    await transaction.CommitAsync(ct);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Database operation {Operation} failed in {Repository}.", operationName, GetType().Name);
            throw;
        }
    }
}
