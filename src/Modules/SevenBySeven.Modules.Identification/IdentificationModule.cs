using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Barcodes;
using SevenBySeven.Modules.Identification.Discogs;
using SevenBySeven.Modules.Identification.Vision;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Identification;

/// <summary>
/// Turns a photograph into Match Candidates: barcode decode first, then vision
/// extraction, then a Discogs search on whatever was read. See docs/adr/0002.
/// Owns no tables and no pages — its whole surface is <see cref="ISleeveIdentifier"/>.
/// </summary>
public sealed class IdentificationModule : IModule
{
    public string Name => "Identification";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentificationOptions>(configuration.GetSection(IdentificationOptions.SectionName));
        services.Configure<DiscogsOptions>(configuration.GetSection(DiscogsOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<IdentificationOptions>>().Value;

            // A bare client still resolves credentials from the environment, so only
            // override when a key was configured explicitly.
            return options.IsConfigured
                ? new AnthropicClient { ApiKey = options.AnthropicApiKey }
                : new AnthropicClient();
        });

        services.AddTransient<DiscogsRateLimitHandler>();

        services.AddHttpClient<IDiscogsCatalogue, DiscogsCatalogue>((provider, http) =>
            {
                var options = provider.GetRequiredService<IOptions<DiscogsOptions>>().Value;

                http.BaseAddress = options.BaseAddress;

                // Discogs rejects a generic user agent, but a misconfigured one must not
                // take the page down: ParseAdd throws on anything not a valid product token.
                if (!http.DefaultRequestHeaders.UserAgent.TryParseAdd(options.UserAgent))
                {
                    provider.GetRequiredService<ILogger<IdentificationModule>>()
                        .LogWarning(
                            "Discogs user agent '{UserAgent}' is not a valid HTTP product token; using the default.",
                            options.UserAgent);

                    http.DefaultRequestHeaders.UserAgent.ParseAdd(new DiscogsOptions().UserAgent);
                }

                if (options.IsConfigured)
                {
                    http.DefaultRequestHeaders.Add(
                        "Authorization", $"Discogs token={options.PersonalAccessToken}");
                }
            })
            .AddHttpMessageHandler<DiscogsRateLimitHandler>();

        services.AddSingleton<IBarcodeScanner, ZXingBarcodeScanner>();
        services.AddScoped<ISleeveReader, ClaudeSleeveReader>();
        services.AddScoped<ISleeveIdentifier, SleeveIdentifier>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
    }
}
