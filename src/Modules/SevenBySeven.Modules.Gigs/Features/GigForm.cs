using System.ComponentModel.DataAnnotations;
using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Modules.Gigs.Features;

/// <summary>
/// The shape a Gig's details take on a page. Shared by recording a Gig and editing one,
/// so the two cannot drift apart.
/// </summary>
public sealed class GigForm
{
    [Required(ErrorMessage = "A gig needs the date it was played.")]
    public DateOnly? PlayedOn { get; set; }

    [StringLength(200, ErrorMessage = "A venue can be at most 200 characters.")]
    public string? Venue { get; set; }

    [StringLength(4000, ErrorMessage = "Notes can be at most 4,000 characters.")]
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
}
