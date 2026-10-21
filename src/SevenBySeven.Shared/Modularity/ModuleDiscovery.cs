using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace SevenBySeven.Shared.Modularity;

/// <summary>
/// Finds the modules composed into a host from the projects it references. Adding a module
/// means adding its project under <c>src/Modules</c>; nothing in a host knows the modules
/// individually.
/// </summary>
public static class ModuleDiscovery
{
    /// <summary>
    /// Finds every <see cref="IModule"/> in the projects <paramref name="host"/> references.
    /// The compiler drops references to assemblies the host never names, so the host's
    /// dependency manifest is read instead of its referenced assemblies. It is loaded for the
    /// host's assembly rather than the entry assembly, which is the EF tooling at design time.
    /// </summary>
    public static IReadOnlyList<IModule> FindIn(Assembly host)
    {
        ArgumentNullException.ThrowIfNull(host);

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
