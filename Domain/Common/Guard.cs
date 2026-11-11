namespace Domain.Common;

internal static class Guard
{
    public static string Required(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} must be at most {maxLength} characters.", paramName);

        return trimmed;
    }

    public static string? Optional(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} must be at most {maxLength} characters.", paramName);

        return trimmed;
    }

    public static string Slug(string? value, string paramName)
    {
        var slug = Required(value, paramName, FieldLengths.Slug).ToLowerInvariant();
        if (slug.Any(char.IsWhiteSpace))
            throw new ArgumentException($"{paramName} cannot contain spaces.", paramName);

        return slug;
    }

    public static decimal Money(decimal value, string paramName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, "Amount cannot be negative.");

        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");

        return value;
    }

    public static byte Rating(byte value)
    {
        if (value is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Rating must be between 1 and 5.");

        return value;
    }
}
