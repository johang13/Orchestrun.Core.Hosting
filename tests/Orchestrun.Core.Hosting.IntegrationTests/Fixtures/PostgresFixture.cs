namespace Orchestrun.Core.Hosting.IntegrationTests.Fixtures;
using Testcontainers.PostgreSql;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public string? ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("orchestrundb_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}