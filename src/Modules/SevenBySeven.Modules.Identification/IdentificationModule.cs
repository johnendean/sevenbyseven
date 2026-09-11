using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Identification;

/// <summary>
/// Turns a photograph into Match Candidates: barcode decode first, then vision
/// extraction, then a Discogs search on whatever was read. See docs/adr/0002.
/// Owns no tables.
/// </summary>
public sealed class IdentificationModule : IModule
{
    public string Name => "Identification";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
    }
}
