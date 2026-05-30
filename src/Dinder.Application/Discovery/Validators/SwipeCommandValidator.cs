using Dinder.Application.Discovery.Commands;
using FluentValidation;

namespace Dinder.Application.Discovery.Validators;

public sealed class SwipeCommandValidator : AbstractValidator<SwipeCommand>
{
    public SwipeCommandValidator()
    {
        RuleFor(x => x.SwiperId)
            .NotEmpty().WithMessage("Swiper ID is required.");

        RuleFor(x => x.SwipedId)
            .NotEmpty().WithMessage("Swiped user ID is required.")
            .NotEqual(x => x.SwiperId).WithMessage("Cannot swipe on yourself.");

        RuleFor(x => x.Direction)
            .IsInEnum().WithMessage("Valid swipe direction is required.");
    }
}
