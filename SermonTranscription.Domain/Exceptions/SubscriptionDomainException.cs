namespace SermonTranscription.Domain.Exceptions;

/// <summary>
/// Exception for subscription-specific domain rule violations
/// </summary>
public class SubscriptionDomainException : DomainException
{
    public SubscriptionDomainException(string message) : base(message)
    {
    }

    public SubscriptionDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
