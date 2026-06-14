using Dinder.Application.Moderation.Commands;
using FluentValidation;

namespace Dinder.Application.Moderation.Validators;

public sealed class ReportUserCommandValidator : AbstractValidator<ReportUserCommand>
{
    public ReportUserCommandValidator()
    {
        RuleFor(x => x.ReporterId)
            .NotEmpty();

        RuleFor(x => x.ReportedUserId)
            .NotEmpty()
            .NotEqual(x => x.ReporterId)
            .WithMessage("You cannot report yourself.");

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithMessage("Invalid report reason.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
