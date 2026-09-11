namespace SevenBySeven.Modules.Identification.Vision;

/// <summary>
/// The sleeve could not be read at all: the call failed, or was never made. This is a
/// different thing from reading a sleeve and finding nothing legible on it, which is an
/// empty <see cref="Domain.SleeveDetails"/> and an honest answer. Conflating the two
/// tells the user to photograph the centre label when the fault is ours.
/// </summary>
public sealed class SleeveReadException(string message, Exception? innerException = null)
    : Exception(message, innerException);
