using Dinder.Application.Moderation.Validators;
using Dinder.Application.Moderation.Commands;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ModerationValidatorTests
{
    [Fact]
    public void ReportUserCommandValidator_Valid_DontThrow()
    {
        var validator = new ReportUserCommandValidator();
        var command = new ReportUserCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Harassment, null, "Bad behavior.");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReportUserCommandValidator_SelfReport_Invalid()
    {
        var userId = Guid.NewGuid();
        var validator = new ReportUserCommandValidator();
        var command = new ReportUserCommand(userId, userId, ReportReason.Spam, null, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("yourself"));
    }

    [Fact]
    public void ReportUserCommandValidator_NoReason_Invalid()
    {
        var validator = new ReportUserCommandValidator();
        var command = new ReportUserCommand(Guid.NewGuid(), Guid.NewGuid(), (ReportReason)99, null, "What?");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ReportUserCommandValidator_LongDescription_Valid()
    {
        var validator = new ReportUserCommandValidator();
        var command = new ReportUserCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportReason.Other,
            null,
            new string('a', 1000)); // Max allowed

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
