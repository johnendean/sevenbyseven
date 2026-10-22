using System.Reflection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Web;

/// <summary>
/// The modules composed into this host, discovered from the projects it references. Only
/// Scanning still needs them in this process; everything else goes through the API.
/// </summary>
internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> All { get; } = ModuleDiscovery.FindIn(typeof(ModuleCatalog).Assembly);

    /// <summary>
    /// Assemblies the router scans for routable components, for Scanning's pages, which still
    /// arrive with the module.
    /// </summary>
    public static IReadOnlyList<Assembly> RoutableAssemblies { get; } =
        All.Select(module => module.GetType().Assembly)
            .Distinct()
            .ToArray();
}
