using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Modules.Identification;
using SevenBySeven.Modules.Scanning;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Web;

/// <summary>
/// The modules composed into this host. Adding a module is a one-line change here;
/// nothing else in the host knows the modules individually.
/// </summary>
internal static class ModuleCatalog
{
    public static IReadOnlyList<IModule> All { get; } =
    [
        new CatalogueModule(),
        new CollectionModule(),
        new IdentificationModule(),
        new ScanningModule(),
    ];
}
