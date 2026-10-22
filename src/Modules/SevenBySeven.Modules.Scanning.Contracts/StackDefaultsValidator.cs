using FluentValidation;

namespace SevenBySeven.Modules.Scanning.Contracts;

/// <summary>
/// The defaults go onto every Copy the Stack adds, so they are held to the same limits a
/// Copy's details are.
/// </summary>
public sealed class StackDefaultsValidator : AbstractValidator<StackDefaults>
{
    public StackDefaultsValidator()
    {
        RuleFor(defaults => defaults.PurchasedFrom)
            .MaximumLength(200)
            .WithMessage("Where it was bought can be at most 200 characters.");

        RuleFor(defaults => defaults.Location)
            .MaximumLength(200)
            .WithMessage("A location can be at most 200 characters.");
    }
}
