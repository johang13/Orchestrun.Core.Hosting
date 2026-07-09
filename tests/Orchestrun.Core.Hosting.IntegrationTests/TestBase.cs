using Microsoft.Extensions.Configuration;

namespace Orchestrun.Core.Hosting.IntegrationTests;

public class TestBase
{
    protected readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();
}