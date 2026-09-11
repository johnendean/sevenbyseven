using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Catalogue;

/// <summary>
/// Owns the locally held copy of externally sourced facts: Releases and their Tracks.
/// Filled on Confirmation through <see cref="ICatalogueSource"/>, read by Collection.
/// </summary>
public sealed class CatalogueModule : IModule
{
    public string Name => "Catalogue";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddScoped<IReleaseCatalogue, ReleaseCatalogue>();

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogueModule).Assembly);
}
