using Dinder.Application.Profile.Commands;
using FluentValidation;

namespace Dinder.Application.Profile.Validators;

public sealed class CreateOrUpdateProfileCommandValidator : AbstractValidator<CreateOrUpdateProfileCommand>
{
    public CreateOrUpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(1).WithMessage("Display name must be at least 1 character.")
            .MaximumLength(100).WithMessage("Display name must be at most 100 characters.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("A valid gender is required.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio must be at most 500 characters.");
    }
}

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.InterestedInGenders)
            .NotEmpty().WithMessage("At least one gender preference is required.");

        RuleFor(x => x.MinAge)
            .InclusiveBetween(18, 100).WithMessage("Minimum age must be between 18 and 100.");

        RuleFor(x => x.MaxAge)
            .InclusiveBetween(18, 100).WithMessage("Maximum age must be between 18 and 100.")
            .GreaterThanOrEqualTo(x => x.MinAge).WithMessage("Maximum age must be greater than or equal to minimum age.");

        RuleFor(x => x.MaxDistanceKm)
            .InclusiveBetween(1, 500).WithMessage("Maximum distance must be between 1 and 500 km.");
    }
}

public sealed class UpdateProfileLocationCommandValidator : AbstractValidator<UpdateProfileLocationCommand>
{
    public UpdateProfileLocationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
    }
}
