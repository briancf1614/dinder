using Dinder.Application.Identity.Commands;
using FluentValidation;

namespace Dinder.Application.Identity.Validators;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Birthday)
            .Must(b => !b.HasValue || IsAtLeast18(b.Value))
            .WithMessage("You must be at least 18 years old to register.");
    }

    private static bool IsAtLeast18(DateOnly birthday)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthday.Year;
        if (birthday > today.AddYears(-age))
            age--;
        return age >= 18;
    }
}
