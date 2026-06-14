using Dinder.Application.Profile.Commands;
using Dinder.Application.Profile.Validators;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ProfilePromptTests
{
    // ── Validation: UpdateProfilePromptsCommand ──────────────────────────

    [Fact]
    public void UpdatePromptsValidator_ValidPrompts_Passes()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), "I love hiking!"),
                new(Guid.NewGuid(), "My favorite food is pizza"),
                new(Guid.NewGuid(), "Looking for someone adventurous"),
            });

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdatePromptsValidator_ExceedsMaxThree_Fails()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), "Answer 1"),
                new(Guid.NewGuid(), "Answer 2"),
                new(Guid.NewGuid(), "Answer 3"),
                new(Guid.NewGuid(), "Answer 4"),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Maximum 3 prompts"));
    }

    [Fact]
    public void UpdatePromptsValidator_EmptyPromptsList_Allows()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdatePromptsValidator_AnswerExceeds150Chars_Fails()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var longAnswer = new string('x', 151);
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), longAnswer),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("150"));
    }

    [Fact]
    public void UpdatePromptsValidator_AnswerExactly150Chars_Passes()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var exactAnswer = new string('x', 150);
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), exactAnswer),
            });

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdatePromptsValidator_EmptyAnswer_Fails()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), ""),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void UpdatePromptsValidator_WhitespaceAnswer_Fails()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.NewGuid(), "   "),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdatePromptsValidator_EmptyPromptId_Fails()
    {
        var validator = new UpdateProfilePromptsCommandValidator();
        var command = new UpdateProfilePromptsCommand(
            Guid.NewGuid(),
            new List<PromptItem>
            {
                new(Guid.Empty, "Valid answer"),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Prompt ID"));
    }

    // ── Validation: CreateOrUpdateProfileCommand with Prompts ────────────

    [Fact]
    public void CreateOrUpdateValidator_ValidPrompts_Passes()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(),
            "TestUser",
            Gender.Female,
            "My bio",
            new List<ProfilePromptDto>
            {
                new(Guid.NewGuid(), "I love coding"),
                new(Guid.NewGuid(), "Coffee enthusiast"),
            });

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateOrUpdateValidator_PromptsExceedMax3_Fails()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(),
            "TestUser",
            Gender.Female,
            "My bio",
            new List<ProfilePromptDto>
            {
                new(Guid.NewGuid(), "A"),
                new(Guid.NewGuid(), "B"),
                new(Guid.NewGuid(), "C"),
                new(Guid.NewGuid(), "D"),
            });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Maximum 3 prompts"));
    }

    [Fact]
    public void CreateOrUpdateValidator_NoPrompts_Passes()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(),
            "TestUser",
            Gender.Male,
            "Bio here",
            null);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    // ── Entity: Profile Prompt Management ───────────────────────────────

    [Fact]
    public void Profile_SetPrompts_ReplacesAllPrompts()
    {
        var profile = new Profile(Guid.NewGuid(), "Alice", Gender.Female, new DateOnly(1995, 1, 1));

        var prompts = new List<ProfilePrompt>
        {
            new(Guid.NewGuid(), "First answer", 0),
            new(Guid.NewGuid(), "Second answer", 1),
            new(Guid.NewGuid(), "Third answer", 2),
        };

        profile.SetPrompts(prompts);

        Assert.Equal(3, profile.Prompts.Count);
        Assert.Contains(profile.Prompts, p => p.Answer == "First answer");
        Assert.Contains(profile.Prompts, p => p.Answer == "Second answer");
        Assert.Contains(profile.Prompts, p => p.Answer == "Third answer");
    }

    [Fact]
    public void Profile_SetPrompts_OverwritesExisting()
    {
        var profile = new Profile(Guid.NewGuid(), "Bob", Gender.Male, new DateOnly(1990, 6, 1));
        profile.SetPrompts(new List<ProfilePrompt>
        {
            new(Guid.NewGuid(), "Old prompt", 0),
        });

        // Replace
        profile.SetPrompts(new List<ProfilePrompt>
        {
            new(Guid.NewGuid(), "New prompt", 0),
        });

        Assert.Single(profile.Prompts);
        Assert.Equal("New prompt", profile.Prompts.First().Answer);
    }

    [Fact]
    public void Profile_ReorderPrompts_UpdatesOrder()
    {
        var profile = new Profile(Guid.NewGuid(), "Carol", Gender.Female, new DateOnly(1993, 3, 15));
        var prompt1 = new ProfilePrompt(Guid.NewGuid(), "First", 0);
        var prompt2 = new ProfilePrompt(Guid.NewGuid(), "Second", 1);
        var prompt3 = new ProfilePrompt(Guid.NewGuid(), "Third", 2);
        profile.SetPrompts(new List<ProfilePrompt> { prompt1, prompt2, prompt3 });

        profile.ReorderPrompts(new List<Guid> { prompt3.PromptId, prompt1.PromptId, prompt2.PromptId });

        Assert.Equal(0, prompt3.Order);
        Assert.Equal(1, prompt1.Order);
        Assert.Equal(2, prompt2.Order);
    }

    [Fact]
    public void ProfilePrompt_Constructor_SetsProperties()
    {
        var promptId = Guid.NewGuid();
        var answer = "I enjoy traveling";
        var order = 5;

        var prompt = new ProfilePrompt(promptId, answer, order);

        Assert.Equal(promptId, prompt.PromptId);
        Assert.Equal(answer, prompt.Answer);
        Assert.Equal(5, prompt.Order);
    }

    [Fact]
    public void ProfilePrompt_SetOrder_ChangesOrder()
    {
        var prompt = new ProfilePrompt(Guid.NewGuid(), "Test answer", 0);
        
        prompt.SetOrder(3);
        
        Assert.Equal(3, prompt.Order);
    }
}
