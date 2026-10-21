using FluentValidation;

namespace SevenBySeven.Modules.Gigs.Contracts;

/// <summary>
/// The limits the database holds a Gig to. The date needs no rule: a body without one does
/// not bind, because <see cref="GigDetails.PlayedOn"/> is required.
/// </summary>
public sealed class GigDetailsValidator : AbstractValidator<GigDetails>
{
    public GigDetailsValidator()
    {
        RuleFor(details => details.Venue)
            .MaximumLength(200)
            .WithMessage("A venue can be at most 200 characters.");

        RuleFor(details => details.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes can be at most 4,000 characters.");
    }
}
