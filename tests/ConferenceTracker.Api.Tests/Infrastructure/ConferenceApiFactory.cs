using ConferenceTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConferenceTracker.Api.Tests.Infrastructure;

/// <summary>
/// Boots the API against a database of its own on the shared Postgres container, freshly migrated
/// and seeded. Runs under the "Testing" environment so the dev-only migrate/seed block in
/// Program.cs stays out of the way and this class owns database setup.
/// </summary>
public sealed class ConferenceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _adminConnectionString;
    private readonly string _databaseName = $"conference_{Guid.NewGuid():N}";

    public ConferenceApiFactory(PostgresFixture postgres) =>
        _adminConnectionString = postgres.ConnectionString;

    private string DatabaseConnectionString =>
        new NpgsqlConnectionStringBuilder(_adminConnectionString) { Database = _databaseName }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Conference"] = DatabaseConnectionString
            }));
    }

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConferenceDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();

        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
