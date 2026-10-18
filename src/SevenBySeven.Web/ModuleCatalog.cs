using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Web;

/// <summary>
/// The modules composed into this host, discovered from the projects it references.
/// Adding a module means adding its project under <c>src/Modules</c>; nothing in the host
/// knows the modules individually.
/// </summary>
internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> All { get; } = Discover();

    /// <summary>
    /// Assemblies the router scans for routable components. Each module carries its own
    /// pages, so a module's UI arrives with the module.
    /// </summary>
    public static IReadOnlyList<Assembly> RoutableAssemblies { get; } =
        All.Select(module => module.GetType().Assembly)
            .Distinct()
            .ToArray();

    /// <summary>
    /// Finds every <see cref="IModule"/> in the projects this host references. The compiler
    /// drops references to assemblies the host never names, so the host's dependency manifest
    /// is read instead of its referenced assemblies. It is loaded for this assembly rather than
    /// the entry assembly, which is the EF tooling at design time.
    /// </summary>
    private static IModule[] Discover()
    {
        var host = typeof(ModuleCatalog).Assembly;
        var dependencies = DependencyContext.Load(host)
            ?? throw new InvalidOperationException(
                $"No dependency manifest found for {host.GetName().Name}; modules cannot be discovered.");

        return dependencies.RuntimeLibraries
            .Where(library => library.Type == "project")
            .Select(library => Assembly.Load(new AssemblyName(library.Name)))
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IModule).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .ToArray();
    }
}
