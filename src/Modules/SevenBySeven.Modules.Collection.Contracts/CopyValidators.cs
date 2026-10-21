using FluentValidation;

namespace SevenBySeven.Modules.Collection.Contracts;

/// <summary>
/// The limits the database holds a Copy's details to. Past them a save would fail, so a
/// request is refused before it gets that far.
/// </summary>
public sealed class CopyDetailsValidator : AbstractValidator<CopyDetails>
{
    public CopyDetailsValidator()
    {
        RuleFor(details => details.PricePaid)
            .InclusiveBetween(0m, 1_000_000m)
            .WithMessage("A price cannot be negative, or more than a million.");

        RuleFor(details => details.PurchasedFrom)
            .MaximumLength(200)
            .WithMessage("Where it was bought can be at most 200 characters.");

        RuleFor(details => details.Location)
            .MaximumLength(200)
            .WithMessage("A location can be at most 200 characters.");

        RuleFor(details => details.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes can be at most 4,000 characters.");
    }
}

public sealed class AddCopyRequestValidator : AbstractValidator<AddCopyRequest>
{
    public AddCopyRequestValidator()
    {
        RuleFor(request => request.Details)
            .NotNull()
            .WithMessage("A Confirmation needs the details of the Copy, even if every one of them is blank.")
            .SetValidator(new CopyDetailsValidator());
    }
}
