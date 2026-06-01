using Dinder.Application.Chat.Commands;
using FluentValidation;

namespace Dinder.Application.Chat.Validators;

public sealed class UnmatchValidator : AbstractValidator<UnmatchCommand>
{
    public UnmatchValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
