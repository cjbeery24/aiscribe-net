namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an invitation email to a user
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="organizationName">Name of the organization</param>
    /// <param name="invitedByName">Name of the person who sent the invitation</param>
    /// <param name="invitationToken">Unique invitation token</param>
    /// <param name="message">Optional personal message</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string toName,
        string organizationName,
        string invitedByName,
        string invitationToken,
        string? message = null);

    /// <summary>
    /// Send a password reset email to a user
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="resetToken">Password reset token</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);

    /// <summary>
    /// Send a welcome email to a new user
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="organizationName">Name of the organization they joined</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string toName, string organizationName);
}
