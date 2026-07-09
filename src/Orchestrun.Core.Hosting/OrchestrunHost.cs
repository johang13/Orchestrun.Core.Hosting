using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orchestrun.Core.Hosting;

/// <summary>
/// Provides a host-specific implementation for building and running an Orchestrun application.
/// </summary>
public sealed class OrchestrunHost
{
    /// <summary>
    /// Creates and returns an instance of <see cref="HostBuilder"/> initialized with the provided arguments.
    /// </summary>
    /// <param name="args">An array of command-line arguments to configure the underlying application builder.</param>
    /// <returns>An instance of <see cref="HostBuilder"/> to configure and build the application.</returns>
    public static HostBuilder CreateBuilder(string[]? args = null)
    {
        args  ??= [];
        var inner = WebApplication.CreateBuilder(args);
        return new HostBuilder(inner);
    }
}