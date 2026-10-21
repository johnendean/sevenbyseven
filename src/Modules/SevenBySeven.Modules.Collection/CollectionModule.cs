using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Modules.Collection.Contracts;
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
        services.AddScoped<IVinylCollection, VinylCollection>();

        services.AddSingleton<IValidator<CopyDetails>, CopyDetailsValidator>();
        services.AddSingleton<IValidator<AddCopyRequest>, AddCopyRequestValidator>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollectionModule).Assembly);

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        CollectionEndpoints.Map(endpoints);
}
