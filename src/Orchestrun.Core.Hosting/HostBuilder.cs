using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orchestrun.Core.Hosting;

/// <summary>
/// A builder for configuring and constructing a <see cref="WebApplication"/>
/// in the context of the Orchestrun framework. This extends the functionality
/// provided by the base <see cref="HostBuilderBase{TBuilder}"/> class.
/// </summary>
public sealed class HostBuilder : HostBuilderBase<HostBuilder>
{
    private readonly WebApplicationBuilder _inner;

    internal HostBuilder(WebApplicationBuilder inner) : base(inner) 
        => _inner = inner;

    /// <summary>
    /// Builds and configures a <see cref="WebApplication"/> for the Orchestrun framework.
    /// This method finalizes the host setup by invoking core configuration steps,
    /// constructing the underlying <see cref="WebApplication"/>, and applying Orchestrun-specific defaults.
    /// </summary>
    /// <returns>A fully built and configured <see cref="WebApplication"/> instance
    /// ready to execute within the Orchestrun hosting environment.</returns>
    public WebApplication Build()
    {
        BuildCore();
        var app = _inner.Build();
        app.AddOrchestrunDefaults();
        return app;
    }
}