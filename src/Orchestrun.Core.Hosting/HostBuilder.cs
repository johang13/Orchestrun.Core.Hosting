using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orchestrun.Core.Hosting;

public sealed class HostBuilder : HostBuilderBase<HostBuilder>
{
    private readonly WebApplicationBuilder _inner;

    internal HostBuilder(WebApplicationBuilder inner) : base(inner) 
        => _inner = inner;

    public WebApplication Build()
    {
        BuildCore();
        var app = _inner.Build();
        app.AddOrchestrunDefaults();
        return app;
    }
}