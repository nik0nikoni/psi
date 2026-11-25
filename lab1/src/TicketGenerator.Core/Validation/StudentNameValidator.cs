namespace TicketGenerator.Core.Validation;

public sealed class StudentNameValidator
{
    public bool TryValidate(string? value, out string normalizedValue)
    {
        normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length > 0;
    }
}
