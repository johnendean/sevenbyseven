using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Identification;
using SevenBySeven.Modules.Identification.Contracts;

namespace SevenBySeven.Tests.Api;

/// <summary>
/// The real API host, in memory, over a database of its own. Only what reaches outside the
/// process is stood in for: Discogs, through the Catalogue's source, and the identification
/// pipeline, whose own tests already drive it against fakes.
/// </summary>
internal sealed class ApiHost : WebApplicationFactory<Program>
{
    /// <summary>How the API writes JSON, so a test reads it the way any client would.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TestDatabase _database;
    private readonly (string Key, string Value)[] _settings;

    private ApiHost(TestDatabase database, (string Key, string Value)[] settings)
    {
        _database = database;
        _settings = settings;
    }

    public TestDatabase Database => _database;

    /// <summary>Stands in for Discogs when the Catalogue fetches a Release.</summary>
    public StubCatalogueSource Source { get; } = new();

    public StubSleeveIdentifier Identifier { get; } = new();

    /// <param name="settings">Configuration the test needs beyond the database, such as a smaller Stack.</param>
    public static async Task<ApiHost> StartAsync(params (string Key, string Value)[] settings) =>
        new(await TestDatabase.CreateAsync(), settings);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:sevenbyseven", _database.ConnectionString);

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ICatalogueSource>(Source);
            services.AddSingleton<ISleeveIdentifier>(Identifier);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    internal sealed class StubCatalogueSource : ICatalogueSource
    {
        /// <summary>Releases Discogs does not have, or cannot be reached for.</summary>
        public HashSet<int> Unfetchable { get; } = [];

        /// <summary>Every Discogs release id it was asked for, in order.</summary>
        public List<int> Fetched { get; } = [];

        public Task<Release?> FetchAsync(int discogsReleaseId, CancellationToken cancellationToken = default)
        {
            Fetched.Add(discogsReleaseId);

            return Task.FromResult(Unfetchable.Contains(discogsReleaseId)
                ? null
                : TestDatabase.AnyRelease(discogsReleaseId, "So What", "Freddie Freeloader"));
        }
    }

    internal sealed class StubSleeveIdentifier : ISleeveIdentifier
    {
        public IdentificationResult Answer { get; set; } = IdentificationResult.Nothing();

        public Exception? Throws { get; set; }

        public List<(byte[] Image, string ContentType, Format Format)> Calls { get; } = [];

        public Task<IdentificationResult> IdentifyAsync(
            ReadOnlyMemory<byte> image,
            string contentType,
            Format format = Format.Vinyl,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((image.ToArray(), contentType, format));

            return Throws is { } thrown ? Task.FromException<IdentificationResult>(thrown) : Task.FromResult(Answer);
        }
    }
}

internal static class ApiResponses
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(ApiHost.Json);

        return value ?? throw new InvalidOperationException($"The response held no {typeof(T).Name}.");
    }
}
