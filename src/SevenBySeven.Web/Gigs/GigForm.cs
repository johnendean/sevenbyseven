using System.ComponentModel.DataAnnotations;
using SevenBySeven.Modules.Gigs.Contracts;

namespace SevenBySeven.Web.Gigs;

/// <summary>
/// The shape a Gig's details take on a page. Shared by recording a Gig and editing one,
/// so the two cannot drift apart. The venue and notes are checked by
/// <see cref="GigDetailsValidator"/>, the same rules the API enforces. The date is checked
/// here, because the details cannot be made without one.
/// </summary>
public sealed class GigForm
{
    [Required(ErrorMessage = "A gig needs the date it was played.")]
    public DateOnly? PlayedOn { get; set; }

    public string? Venue { get; set; }

    public string? Notes { get; set; }

    /// <summary>A new Gig, dated today because that is when most are entered.</summary>
    public static GigForm Tonight(DateOnly today) => new() { PlayedOn = today };

    public static GigForm For(GigSheet gig)
    {
        ArgumentNullException.ThrowIfNull(gig);

        return new GigForm { PlayedOn = gig.PlayedOn, Venue = gig.Venue, Notes = gig.Notes };
    }

    /// <summary>Only meaningful once the form has validated, which is what insists on a date.</summary>
    public GigDetails ToDetails() => new()
    {
        PlayedOn = PlayedOn ?? throw new InvalidOperationException("A gig needs the date it was played."),
        Venue = Venue,
        Notes = Notes,
    };

    /// <summary>
    /// The details as they stand, for checking against the API's rules while the form is
    /// being filled in, before it has a date.
    /// </summary>
    public GigDetails Draft() => new() { PlayedOn = PlayedOn ?? default, Venue = Venue, Notes = Notes };
}
