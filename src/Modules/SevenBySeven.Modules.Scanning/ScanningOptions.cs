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
    /// held in the API's memory until they are confirmed or abandoned, so a careless value
    /// here is measured in tens of megabytes.
    /// </summary>
    public const int HighestMaxStackSize = 25;

    /// <summary>Long enough to capture in the afternoon and work through after dinner.</summary>
    public static readonly TimeSpan DefaultStackIdleTimeout = TimeSpan.FromHours(4);

    private static readonly TimeSpan ShortestStackIdleTimeout = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan LongestStackIdleTimeout = TimeSpan.FromDays(1);

    private int _maxStackSize = DefaultMaxStackSize;

    private TimeSpan _stackIdleTimeout = DefaultStackIdleTimeout;

    /// <summary>
    /// How many Scans one Stack may hold before capture stops. Clamped rather than
    /// validated: a Stack that silently caps is better than an API that will not start.
    /// </summary>
    public int MaxStackSize
    {
        get => _maxStackSize;
        set => _maxStackSize = Math.Clamp(value, 1, HighestMaxStackSize);
    }

    /// <summary>
    /// How long a Stack may go without anything being done to it before it is abandoned,
    /// with every Scan it still holds. Nothing else would ever let its photographs go.
    /// Clamped, as the size is.
    /// </summary>
    public TimeSpan StackIdleTimeout
    {
        get => _stackIdleTimeout;
        set => _stackIdleTimeout = value < ShortestStackIdleTimeout
            ? ShortestStackIdleTimeout
            : value > LongestStackIdleTimeout ? LongestStackIdleTimeout : value;
    }
}
