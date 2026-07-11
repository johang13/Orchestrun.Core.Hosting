using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;

namespace Orchestrun.Core.Hosting.Database;

/// <summary>
/// Marks a logical database used by repositories in the host.
/// The type provides the keyed service name used to resolve the matching
/// <see cref="NpgsqlDataSource"/> and <see cref="ResiliencePipeline"/>.
/// </summary>
public interface IDatabaseKey
{
    static abstract string Name { get; }
}

/// <summary>
/// Base class for Postgres repositories that use keyed infrastructure registrations.
/// The repository receives the matching <see cref="NpgsqlDataSource"/> and
/// <see cref="ResiliencePipeline"/> for the database key represented by <typeparamref name="TDbKey"/>.
/// </summary>
public abstract class OrchestrunRepositoryBase<TDbKey>(
    NpgsqlDataSource dataSource,
    ResiliencePipeline pipeline,
    ILogger logger)
    where TDbKey : IDatabaseKey
{
    protected static string ConnectionStringName => TDbKey.Name;
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
            Logger.LogError(ex, "Database operation {Operation} failed for {ConnectionStringName}.", operationName, ConnectionStringName);
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
            Logger.LogError(ex, "Database operation {Operation} failed for {ConnectionStringName}.", operationName, ConnectionStringName);
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
            Logger.LogError(ex, "Database operation {Operation} failed for {ConnectionStringName}.", operationName, ConnectionStringName);
            throw;
        }
    }
}
