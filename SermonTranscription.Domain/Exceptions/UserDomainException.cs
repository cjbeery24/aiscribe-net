namespace SermonTranscription.Domain.Exceptions;

/// <summary>
/// Exception thrown when user-related business rules are violated
/// </summary>
public class UserDomainException : DomainException
{
    public UserDomainException(string message) : base(message)
    {
    }
    
    public UserDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when user authentication fails
/// </summary>
public class UserAuthenticationException : UserDomainException
{
    public UserAuthenticationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when user email verification fails
/// </summary>
public class UserEmailVerificationException : UserDomainException
{
    public UserEmailVerificationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when user password reset fails
/// </summary>
public class UserPasswordResetException : UserDomainException
{
    public UserPasswordResetException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when user permission is denied
/// </summary>
public class UserPermissionException : UserDomainException
{
    public UserPermissionException(string message) : base(message)
    {
    }
} 