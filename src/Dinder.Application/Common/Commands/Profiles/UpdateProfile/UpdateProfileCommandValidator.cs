using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Application.Common.Commands.Profiles.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio must not exceed 500 characters");
            RuleFor(x => x.BirthDate)
                .Must(bd => bd == null || bd.Value < DateOnly.FromDateTime(DateTime.Today))
                    .WithMessage("Birth date must be in the past")
                .Must(bd => bd == null || bd.Value <= DateOnly.FromDateTime(DateTime.Today.AddYears(-18)))
                    .WithMessage("You must be at least 18 years old");
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("Gender must be a valid value");
        }
    }
}
