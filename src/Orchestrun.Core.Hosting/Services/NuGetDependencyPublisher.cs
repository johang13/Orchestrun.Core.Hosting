using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orchestrun.Core.Contracts.Events;

namespace Orchestrun.Core.Hosting.Services;

/// <summary>
/// Background service responsible for publishing NuGet dependency information on service startup.
/// </summary>
/// <param name="publishEndpoint"></param>
/// <param name="logger"></param>
public sealed class NugetDependencyPublisher(IPublishEndpoint publishEndpoint, ILogger<NugetDependencyPublisher> logger)
    : BackgroundService
{
    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // DependencyContext.Default is populated by the runtime at startup from the *running*
        // app's own *.deps.json — no need to locate/read/parse the file by hand.
        var context = DependencyContext.Default
                      ?? throw new InvalidOperationException(
                          "No DependencyContext available. Make sure a *.deps.json file is published next to the entry assembly.");

        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

        // The library representing this project itself; its own Dependencies are the direct
        // references — everything else in RuntimeLibraries is pulled in transitively.
        var projectLibrary = context.RuntimeLibraries.FirstOrDefault(l => l.Type == "project")
                             ?? context.RuntimeLibraries.FirstOrDefault(l =>
                                 string.Equals(l.Name, entryAssemblyName, StringComparison.OrdinalIgnoreCase));

        var directNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (projectLibrary is not null)
            foreach (var dependency in projectLibrary.Dependencies)
                directNames.Add(dependency.Name);

        var payload = new NuGetDependenciesPublished
        {
            ServiceName = entryAssemblyName ?? "Unknown",
            RuntimeTarget = context.Target.Framework,
            Dependencies = context.RuntimeLibraries
                .Where(lib => lib.Type == "package")
                .Select(lib => new NuGetDependencyInfo
                {
                    Name = lib.Name,
                    Version = lib.Version.ToString(),
                    IsDirectDependency = directNames.Contains(lib.Name),
                    Type = lib.Type,
                    Hash = lib.Hash
                }).ToList()
        };

        logger.LogInformation($"Published NuGet dependencies for {payload.ServiceName}");
        return publishEndpoint.Publish(payload, stoppingToken);
    }
}