using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orchestrun.Core.Hosting;

public sealed class OrchestrunHost
{
    public static HostBuilder CreateBuilder(string[] args)
    {
        var inner = WebApplication.CreateBuilder();
        return new HostBuilder(inner);
    }
}