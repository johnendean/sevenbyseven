namespace SevenBySeven.Modules.Gigs.Contracts;

/// <summary>A Copy from my Collection, played at the end of a Selection.</summary>
public sealed record AddPlayRequest(Guid CopyId);

/// <summary>Moves a Play one place within its Selection: earlier, or else later.</summary>
public sealed record MovePlayRequest(bool Earlier);
