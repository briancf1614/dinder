using Dinder.Application.Chat.Commands;
using FluentValidation;

namespace Dinder.Application.Chat.Validators;

public sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(x => x.SenderId)
            .NotEmpty().WithMessage("Sender ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required.")
            .MaximumLength(2000).WithMessage("Message content must be 2000 characters or fewer.");
    }
}
