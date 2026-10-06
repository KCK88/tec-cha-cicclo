using System.Text.RegularExpressions;

namespace Cicclo.Api.Auth;

public static partial class EmailAddress
{
    public static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 254 || !EmailPattern().IsMatch(normalized))
            return null;

        return normalized;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
