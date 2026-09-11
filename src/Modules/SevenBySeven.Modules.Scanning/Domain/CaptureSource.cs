namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>How a Scan's photograph reached us.</summary>
public enum CaptureSource
{
    /// <summary>Live preview via getUserMedia. Needs a secure context.</summary>
    Camera = 1,

    /// <summary>A file the browser supplied, typically the phone's own camera app.</summary>
    File = 2,
}
