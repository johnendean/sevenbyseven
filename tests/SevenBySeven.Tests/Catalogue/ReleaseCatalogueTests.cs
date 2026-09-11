using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Tests.Catalogue;

public class ReleaseCatalogueTests
{
    [Fact]
    public async Task A_pressing_is_fetched_once_and_held_from_then_on()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var source = new StubSource(id => TestDatabase.AnyRelease(id));
        var catalogue = Catalogue(context, source);

        var first = await catalogue.EnsureAsync(249504);
        var second = await catalogue.EnsureAsync(249504);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, source.Calls);

        await using var later = database.NewContext();
        Assert.Equal(1, await later.Set<Release>().CountAsync());
    }

    [Fact]
    public async Task Tracks_are_held_with_their_pressing_and_keep_their_running_order()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var source = new StubSource(id => TestDatabase.AnyRelease(id, "So What", "Freddie Freeloader", "Blue In Green"));

        await Catalogue(context, source).EnsureAsync(249504);

        await using var later = database.NewContext();
        var held = await Catalogue(later, new StubSource(_ => null)).EnsureAsync(249504);

        Assert.NotNull(held);
        Assert.Equal(
            ["So What", "Freddie Freeloader", "Blue In Green"],
            held.Tracks.Select(track => track.Title));
    }

    [Fact]
    public async Task A_pressing_the_source_cannot_supply_is_not_held()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var held = await Catalogue(context, new StubSource(_ => null)).EnsureAsync(249504);

        Assert.Null(held);

        await using var later = database.NewContext();
        Assert.Equal(0, await later.Set<Release>().CountAsync());
    }

    [Fact]
    public async Task A_pressing_cached_by_a_racing_request_is_read_rather_than_duplicated()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        // The other request gets there while we are still fetching, which is exactly what
        // an impatient double-click does.
        var source = new StubSource(id =>
        {
            using var winner = database.NewContext();
            winner.Add(TestDatabase.AnyRelease(id));
            winner.SaveChanges();

            return TestDatabase.AnyRelease(id);
        });

        var held = await Catalogue(context, source).EnsureAsync(249504);

        Assert.NotNull(held);
        Assert.Equal(249504, held.DiscogsReleaseId);

        await using var later = database.NewContext();
        Assert.Equal(1, await later.Set<Release>().CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_release_id_has_to_be_a_release_id(int discogsReleaseId)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Catalogue(context, new StubSource(_ => null)).EnsureAsync(discogsReleaseId));
    }

    private static IReleaseCatalogue Catalogue(SevenBySevenDbContext context, ICatalogueSource source) =>
        new ReleaseCatalogue(context, source, NullLogger<ReleaseCatalogue>.Instance);

    private sealed class StubSource(Func<int, Release?> fetch) : ICatalogueSource
    {
        public int Calls { get; private set; }

        public Task<Release?> FetchAsync(int discogsReleaseId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(fetch(discogsReleaseId));
        }
    }
}
