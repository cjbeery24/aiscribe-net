using System;
using SermonTranscription.Domain.Exceptions;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Service for validating password requirements.
/// </summary>
public class PasswordValidator : IPasswordValidator
{
    private const int MinLength = 8;
    // You can add more requirements here (uppercase, digit, special char, etc.)

    public void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new PasswordValidationDomainException("Password is required.");
        if (password.Length < MinLength)
            throw new PasswordValidationDomainException($"Password must be at least {MinLength} characters long.");
        // Add more rules as needed
    }
}

public interface IPasswordValidator
{
    void Validate(string password);
}
