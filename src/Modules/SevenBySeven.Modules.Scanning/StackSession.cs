using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// Holds the Stack in progress for one user's circuit. Scoped, never persisted: when the
/// circuit ends the photographs go with it, which is the whole storage policy and the
/// reason a Stack survives navigating away from the capture page but not closing the tab.
/// </summary>
public sealed class StackSession
{
    public Stack? Current { get; private set; }

    /// <summary>
    /// What is true of every Copy this Stack adds — where they were bought, where they
    /// are kept, what currency the prices are in. Condition and price vary record by
    /// record and are not here.
    /// </summary>
    public StackDefaults Defaults { get; private set; } = StackDefaults.None;

    public bool HasStack => Current is not null;

    /// <summary>Worth telling me about before I lose it by closing the tab.</summary>
    public bool HasUnresolvedWork => Current is { IsEmpty: false } stack && !stack.IsComplete;

    public event Action? Changed;

    public Stack Begin(int capacity)
    {
        Current = new Stack(capacity);
        Defaults = StackDefaults.None;
        Changed?.Invoke();

        return Current;
    }

    public void SetDefaults(StackDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        Defaults = defaults;
        Changed?.Invoke();
    }

    /// <summary>The Stack is over: whatever it still held is given up on and let go.</summary>
    public void Discard()
    {
        if (Current is null)
        {
            return;
        }

        Current.AbandonRemaining();
        Current = null;
        Defaults = StackDefaults.None;
        Changed?.Invoke();
    }

    /// <summary>Re-renders whatever is watching. Identification finishing is not a
    /// user's action, so nothing else would.</summary>
    public void Touch() => Changed?.Invoke();
}
