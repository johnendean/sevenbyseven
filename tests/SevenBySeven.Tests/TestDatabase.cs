using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Shared.Modularity;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Tests;

/// <summary>
/// A real relational database for the length of one test, held in memory. SQLite rather
/// than Postgres so <c>dotnet test</c> needs no container, and a real provider rather
/// than a fake one so keys, foreign keys and unique indexes are actually enforced —
/// which is the entire point of the tests that use it.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private static readonly IModule[] Modules =
        [new CatalogueModule(), new CollectionModule(), new SqliteQuirks()];

    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection) => _connection = connection;

    public static async Task<TestDatabase> CreateAsync()
    {
        // The database lives as long as the connection does, so this one stays open.
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var database = new TestDatabase(connection);

        await using var context = database.NewContext();
        await context.Database.EnsureCreatedAsync();

        return database;
    }

    /// <summary>
    /// A fresh context over the same database — what a later request would see, with
    /// nothing left in the change tracker to flatter the result.
    /// </summary>
    public SevenBySevenDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<SevenBySevenDbContext>()
                .UseSqlite(_connection)
                .UseSnakeCaseNamingConvention()
                .Options,
            Modules);

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

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    /// <summary>
    /// SQLite stores a DateTimeOffset as text and refuses to order by it, so for tests
    /// the column becomes sortable ticks. Postgres orders a timestamptz natively and
    /// needs none of this — which is why it is a module here rather than a change to
    /// how Collection or Catalogue map themselves.
    /// </summary>
    private sealed class SqliteQuirks : IModule
    {
        private static readonly ValueConverter<DateTimeOffset, long> Sortable =
            new(moment => moment.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

        public string Name => "SQLite quirks";

        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public void ConfigureModel(ModelBuilder modelBuilder)
        {
            var moments = modelBuilder.Model.GetEntityTypes()
                .SelectMany(entity => entity.GetProperties())
                .Where(property => property.ClrType == typeof(DateTimeOffset)
                    || property.ClrType == typeof(DateTimeOffset?));

            foreach (var moment in moments)
            {
                moment.SetValueConverter(Sortable);
            }
        }
    }
}
