namespace SermonTranscription.Domain.Exceptions;

/// <summary>
/// Exception thrown when organization-related business rules are violated
/// </summary>
public class OrganizationDomainException : DomainException
{
    public OrganizationDomainException(string message) : base(message)
    {
    }
    
    public OrganizationDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when organization subscription limits are exceeded
/// </summary>
public class OrganizationSubscriptionLimitException : OrganizationDomainException
{
    public OrganizationSubscriptionLimitException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when organization user limit is exceeded
/// </summary>
public class OrganizationUserLimitException : OrganizationDomainException
{
    public OrganizationUserLimitException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when organization feature is not available
/// </summary>
public class OrganizationFeatureNotAvailableException : OrganizationDomainException
{
    public OrganizationFeatureNotAvailableException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when organization is inactive
/// </summary>
public class OrganizationInactiveException : OrganizationDomainException
{
    public OrganizationInactiveException(string message) : base(message)
    {
    }
} 