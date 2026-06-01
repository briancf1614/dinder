using Dinder.Application.Notifications.Commands;
using Dinder.Application.Notifications.Validators;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class NotificationValidatorTests
{
    [Fact]
    public void RegisterDeviceTokenValidator_ValidCommand_Passes()
    {
        var validator = new RegisterDeviceTokenValidator();
        var command = new RegisterDeviceTokenCommand(Guid.NewGuid(), "valid-fcm-token-123", DevicePlatform.Fcm);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RegisterDeviceTokenValidator_EmptyToken_Fails()
    {
        var validator = new RegisterDeviceTokenValidator();
        var command = new RegisterDeviceTokenCommand(Guid.NewGuid(), "", DevicePlatform.Fcm);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Token");
    }

    [Fact]
    public void RegisterDeviceTokenValidator_EmptyUserId_Fails()
    {
        var validator = new RegisterDeviceTokenValidator();
        var command = new RegisterDeviceTokenCommand(Guid.Empty, "token", DevicePlatform.Apns);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UserId");
    }
}
