using SevenBySeven.Modules.Scanning.Contracts;

namespace SevenBySeven.Web.Scanning;

/// <summary>
/// Holds a Scan that stands alone for one user's circuit. Scoped, never persisted: when the
/// circuit ends the photograph goes with it, which is the whole storage policy. A Scan in a
/// Stack is held by the API instead (docs/adr/0010).
/// </summary>
public sealed class ScanSession
{
    public Scan? Current { get; private set; }

    public bool HasScan => Current is not null;

    public event Action? Changed;

    public void Set(Scan scan)
    {
        ArgumentNullException.ThrowIfNull(scan);
        Current = scan;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (Current is null)
        {
            return;
        }

        Current = null;
        Changed?.Invoke();
    }
}
