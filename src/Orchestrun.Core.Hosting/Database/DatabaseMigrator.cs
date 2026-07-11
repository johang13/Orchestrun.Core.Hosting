using System.Reflection;
using DbUp;

namespace Orchestrun.Core.Hosting.Database;

/// <summary>
/// Runs pending SQL migrations (embedded as resources in <paramref name="migrationsAssembly"/>)
/// against a Postgres database using DbUp.
/// </summary>
internal static class DatabaseMigrator
{
    public static void Run(string connectionString, Assembly migrationsAssembly)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(migrationsAssembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException(
                $"Database migration failed for assembly '{migrationsAssembly.GetName().Name}'.",
                result.Error);
    }
}
