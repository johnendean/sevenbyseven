namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// How a Stack behaves. Bound from configuration, so the ceiling can be raised for an
/// afternoon of cataloguing with <c>Scanning__MaxStackSize</c> and put back afterwards.
/// </summary>
public sealed class ScanningOptions
{
    public const string SectionName = "Scanning";

    /// <summary>Ten records is a comfortable sitting of confirmations.</summary>
    public const int DefaultMaxStackSize = 10;

    /// <summary>
    /// The most a Stack may ever hold, whatever configuration asks for. Photographs are
    /// held in the circuit until they are confirmed or abandoned, so a careless value
    /// here is measured in tens of megabytes of memory per user.
    /// </summary>
    public const int HighestMaxStackSize = 25;

    private int _maxStackSize = DefaultMaxStackSize;

    /// <summary>
    /// How many Scans one Stack may hold before capture stops. Clamped rather than
    /// validated: a Stack that silently caps is better than a page that will not load.
    /// </summary>
    public int MaxStackSize
    {
        get => _maxStackSize;
        set => _maxStackSize = Math.Clamp(value, 1, HighestMaxStackSize);
    }

    public DetectionOptions Detection { get; set; } = new();
}
