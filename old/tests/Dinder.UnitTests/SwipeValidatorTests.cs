using Dinder.Application.Discovery.Commands;
using Dinder.Application.Discovery.Validators;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class SwipeValidatorTests
{
    [Fact]
    public void SwipeCommand_Valid_PassesValidation()
    {
        var validator = new SwipeCommandValidator();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SwipeCommand_EmptySwiperId_FailsValidation()
    {
        var validator = new SwipeCommandValidator();
        var command = new SwipeCommand(Guid.Empty, Guid.NewGuid(), SwipeDirection.Right);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SwiperId");
    }

    [Fact]
    public void SwipeCommand_SelfSwipe_FailsValidation()
    {
        var userId = Guid.NewGuid();
        var validator = new SwipeCommandValidator();
        var command = new SwipeCommand(userId, userId, SwipeDirection.Right);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SwipedId");
    }
}
