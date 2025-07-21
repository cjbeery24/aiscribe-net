namespace SermonTranscription.Domain.Exceptions;

/// <summary>
/// Exception thrown when a password does not meet validation requirements.
/// </summary>
public class PasswordValidationDomainException : DomainException
{
    public PasswordValidationDomainException(string message) : base(message) { }
}
