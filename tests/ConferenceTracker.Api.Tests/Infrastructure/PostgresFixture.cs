using Testcontainers.PostgreSql;

namespace ConferenceTracker.Api.Tests.Infrastructure;

/// <summary>
/// One Postgres container for the whole test run — starting it is the dominant cost, so it is
/// shared. Isolation comes from each test class creating its own database on it.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("conference")
        .WithUsername("conference")
        .WithPassword("conference")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(PostgresCollection.Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
