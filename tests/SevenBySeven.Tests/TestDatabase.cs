using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Catalogue.Domain;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Modules.Gigs;
using SevenBySeven.Shared.Modularity;
using SevenBySeven.Shared.Persistence;
using Testcontainers.PostgreSql;

namespace SevenBySeven.Tests;

/// <summary>
/// A database of its own for the length of one test, on the same Postgres the app runs
/// against, built by the same migrations — so keys, foreign keys, unique indexes and column
/// types are the real ones. See docs/adr/0007-tests-run-against-postgres.md.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    /// <summary>
    /// The image the AppHost's Aspire version runs. Move it when Aspire moves, so the tests
    /// keep meeting the server the app does.
    /// </summary>
    private const string Image = "postgres:18.3";

    /// <summary>The migrated, empty database every test's own database is copied from.</summary>
    private const string Template = "sevenbyseven_template";

    private static readonly IModule[] Modules =
        [new CatalogueModule(), new CollectionModule(), new GigsModule()];

    /// <summary>
    /// One container for the whole run, started by whichever test gets here first. It is
    /// never disposed here: Testcontainers' reaper removes it once the run has ended.
    /// </summary>
    private static readonly Lazy<Task<PostgreSqlContainer>> Server = new(StartAsync);

    // Postgres refuses to copy a template while anything else is connected to it, so the
    // copies are made one at a time. Each takes milliseconds.
    private static readonly SemaphoreSlim Copying = new(1, 1);

    private readonly PostgreSqlContainer _server;
    private readonly string _name;
    private readonly string _connectionString;

    private TestDatabase(PostgreSqlContainer server, string name)
    {
        _server = server;
        _name = name;
        _connectionString = ConnectionStringFor(server, name);
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var server = await Server.Value;
        var name = $"test_{Guid.NewGuid():N}";

        await Copying.WaitAsync();

        try
        {
            await ExecuteAsync(server, $"""CREATE DATABASE "{name}" TEMPLATE "{Template}" """);
        }
        finally
        {
            Copying.Release();
        }

        return new TestDatabase(server, name);
    }

    /// <summary>
    /// A fresh context over the same database — what a later request would see, with
    /// nothing left in the change tracker to flatter the result.
    /// </summary>
    public SevenBySevenDbContext NewContext(params IInterceptor[] interceptors) =>
        NewContext(_connectionString, interceptors);

    public static Release AnyRelease(int discogsReleaseId = 249504, params string[] trackTitles)
    {
        var release = new Release
        {
            DiscogsReleaseId = discogsReleaseId,
            Title = "Kind Of Blue",
            ArtistName = "Miles Davis",
            LabelName = "Columbia",
            CatalogueNumber = "CS 8163",
            Country = "US",
            Released = ReleaseDate.ForYear(1959),
            FormatDescription = "Vinyl, LP, Album",
        };

        release.ReplaceTracks(trackTitles.Select((title, index) => new Track
        {
            Position = $"A{index + 1}",
            Title = title,
            Sequence = index,
        }));

        return release;
    }

    /// <summary>The real play history over a context, with the Repeat window as configured.</summary>
    public static IPlayHistory PlayHistory(SevenBySevenDbContext context, int repeatWindow = GigsOptions.DefaultRepeatWindow) =>
        new PlayHistory(context, Options.Create(new GigsOptions { RepeatWindow = repeatWindow }));

    /// <summary>
    /// Drops the database rather than leaving it for the container to take with it, so its
    /// pooled connections do not pile up against the server's connection limit over a run.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            NpgsqlConnection.ClearPool(connection);
        }

        await ExecuteAsync(_server, $"""DROP DATABASE "{_name}" WITH (FORCE)""");
    }

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        // Without a container runtime this throws Testcontainers' own "Docker is either not
        // running or misconfigured", and every database test fails with it rather than
        // skipping: a skip would leave the coverage gate red for no visible reason.
        var server = new PostgreSqlBuilder(Image).Build();
        await server.StartAsync();

        await ExecuteAsync(server, $"""CREATE DATABASE "{Template}" """);

        var template = ConnectionStringFor(server, Template);

        await using (var context = NewContext(template))
        {
            await context.Database.MigrateAsync();
        }

        // Copying refuses a template with anyone still connected, and the migration's
        // connection would otherwise stay open in the pool.
        await using (var connection = new NpgsqlConnection(template))
        {
            NpgsqlConnection.ClearPool(connection);
        }

        return server;
    }

    private static SevenBySevenDbContext NewContext(string connectionString, params IInterceptor[] interceptors) =>
        new(
            new DbContextOptionsBuilder<SevenBySevenDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(interceptors)
                .Options,
            Modules);

    private static string ConnectionStringFor(PostgreSqlContainer server, string database) =>
        new NpgsqlConnectionStringBuilder(server.GetConnectionString()) { Database = database }.ConnectionString;

    /// <summary>Runs a statement against the server's own database, outside any test's.</summary>
    private static async Task ExecuteAsync(PostgreSqlContainer server, string sql)
    {
        await using var connection = new NpgsqlConnection(server.GetConnectionString());
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
