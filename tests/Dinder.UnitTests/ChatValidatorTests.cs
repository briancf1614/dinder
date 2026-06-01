using Dinder.Application.Chat.Commands;
using Dinder.Application.Chat.Validators;
using Xunit;

namespace Dinder.UnitTests;

public class ChatValidatorTests
{
    [Fact]
    public void SendMessageValidator_ValidCommand_Passes()
    {
        var validator = new SendMessageValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "Hello!");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SendMessageValidator_EmptyContent_Fails()
    {
        var validator = new SendMessageValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }

    [Fact]
    public void SendMessageValidator_ContentExceeds2000Chars_Fails()
    {
        var validator = new SendMessageValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('A', 2001));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }

    [Fact]
    public void SendMessageValidator_ContentExactly2000Chars_Passes()
    {
        var validator = new SendMessageValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('A', 2000));

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SendMessageValidator_EmptyConversationId_Fails()
    {
        var validator = new SendMessageValidator();
        var command = new SendMessageCommand(Guid.Empty, Guid.NewGuid(), "Hi");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConversationId");
    }

    [Fact]
    public void UnmatchValidator_ValidCommand_Passes()
    {
        var validator = new UnmatchValidator();
        var command = new UnmatchCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UnmatchValidator_EmptyConversationId_Fails()
    {
        var validator = new UnmatchValidator();
        var command = new UnmatchCommand(Guid.Empty, Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConversationId");
    }
}
