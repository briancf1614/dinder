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

        When(x => x.Prompts is not null, () =>
        {
            RuleFor(x => x.Prompts!.Count)
                .LessThanOrEqualTo(3)
                .WithMessage("Maximum 3 prompts allowed.");

            RuleForEach(x => x.Prompts).ChildRules(prompts =>
            {
                prompts.RuleFor(p => p.Answer)
                    .NotEmpty().WithMessage("Prompt answer is required.")
                    .MaximumLength(150).WithMessage("Prompt answer must be at most 150 characters.");
            });
        });
    }
}

public sealed class UpdateProfilePromptsCommandValidator : AbstractValidator<UpdateProfilePromptsCommand>
{
    public UpdateProfilePromptsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Prompts)
            .NotNull().WithMessage("Prompts list is required.");

        RuleFor(x => x.Prompts.Count)
            .LessThanOrEqualTo(3)
            .WithMessage("Maximum 3 prompts allowed.");

        RuleForEach(x => x.Prompts).ChildRules(prompts =>
        {
            prompts.RuleFor(p => p.PromptId)
                .NotEmpty().WithMessage("Prompt ID is required.");

            prompts.RuleFor(p => p.Answer)
                .NotEmpty().WithMessage("Prompt answer is required.")
                .MaximumLength(150).WithMessage("Prompt answer must be at most 150 characters.");
        });
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
