using SevenBySeven.Modules.Scanning.Contracts;
using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Modules.Scanning;

/// <summary>The Stack as the API shows it: everything but the photographs.</summary>
internal static class StackContracts
{
    public static StackSheet ToSheet(this Stack stack, StackDefaults defaults) =>
        new(stack.Id, stack.Capacity, stack.Format, defaults, [.. stack.Scans.Select(scan => scan.ToLine())]);

    public static StackedScanLine ToLine(this StackedScan scan) =>
        new(scan.Id, scan.Position, scan.Status, scan.Result, scan.Failure);
}
