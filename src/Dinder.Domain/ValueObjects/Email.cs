using System.Text.RegularExpressions;

namespace Dinder.Domain.ValueObjects;

public sealed partial record Email
{
    private static readonly Regex Pattern = EmailRegex();

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        if (!Pattern.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);

    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
