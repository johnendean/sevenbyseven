namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// The thresholds the browser's record detector runs on. They are here, rather than in
/// the script, because they have no correct value derivable in advance — they were found
/// by running the detector against real records in real lighting, and a different room
/// may want different ones (docs/adr/0005).
/// </summary>
public sealed class DetectionOptions
{
    /// <summary>How far a pixel must differ from the empty frame to count as something.</summary>
    public int ForegroundDelta { get; set; } = 28;

    /// <summary>How much of the frame something must fill before it is worth looking at.</summary>
    public double MinCoverage { get; set; } = 0.12;

    /// <summary>Below this, the frame is treated as clear.</summary>
    public double EmptyCoverage { get; set; } = 0.04;

    /// <summary>A record is a solid rectangle. A waving hand is not.</summary>
    public double MinFill { get; set; } = 0.45;

    /// <summary>Sleeve art and print vary. A blank surface does not.</summary>
    public double MinContrast { get; set; } = 18;

    /// <summary>How much the picture may change between frames and still count as held still.</summary>
    public double MotionThreshold { get; set; } = 2.5;

    /// <summary>Frames of stillness before a capture fires. Roughly fifteen to the second.</summary>
    public int StillFrames { get; set; } = 8;

    /// <summary>Frames the view must be clear before the next record can be captured.</summary>
    public int EmptyFrames { get; set; } = 6;

    /// <summary>Frames of a still, empty scene before it is trusted as the reference.</summary>
    public int CalibrateFrames { get; set; } = 10;

    /// <summary>
    /// A fraction of how full the frame was when the capture fired. The record counts as
    /// gone once less than this much of it remains. Relative rather than absolute because
    /// an arm stays in shot and the camera re-exposes when a sleeve fills the lens, so an
    /// absolute reading never returns to where it started.
    /// </summary>
    public double ClearFraction { get; set; } = 0.35;

    /// <summary>
    /// Frames before something in view that is not a record, and never moves, is treated
    /// as part of the room. Without it, a shifted background stalls capture indefinitely.
    /// </summary>
    public int SettleOutFrames { get; set; } = 25;
}
