using Dinder.Application.Notifications.Commands;
using FluentValidation;

namespace Dinder.Application.Notifications.Validators;

public sealed class RegisterDeviceTokenValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Device token is required.")
            .MaximumLength(512).WithMessage("Device token must be 512 characters or fewer.");

        RuleFor(x => x.Platform)
            .IsInEnum().WithMessage("Platform must be Fcm or Apns.");
    }
}
