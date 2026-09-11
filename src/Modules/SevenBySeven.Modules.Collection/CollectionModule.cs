using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Collection;

/// <summary>
/// Owns the Copies I own, and the pages for browsing them.
/// Reads the Catalogue; never writes to it.
/// </summary>
public sealed class CollectionModule : IModule
{
    public string Name => "Collection";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollectionModule).Assembly);
}
