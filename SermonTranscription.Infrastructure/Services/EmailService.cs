using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Infrastructure.Services;

/// <summary>
/// Email service implementation that logs emails for development/testing
/// In production, this would integrate with SendGrid, Mailgun, or similar
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _frontendBaseUrl;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _frontendBaseUrl = configuration["App:FrontendBaseUrl"] ?? "https://yourapp.com";
    }

    public async Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string toName,
        string organizationName,
        string invitedByName,
        string invitationToken,
        string? message = null)
    {
        try
        {
            // In production, this would send an actual email
            // For now, we'll log the email content for development/testing

            var emailContent = GenerateInvitationEmailContent(toName, organizationName, invitedByName, invitationToken, message, _frontendBaseUrl);

            _logger.LogInformation(
                "INVITATION EMAIL SENT to {Email} for {Organization}: {Content}",
                toEmail,
                organizationName,
                emailContent);

            // Simulate async email sending
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
    {
        try
        {
            var emailContent = GeneratePasswordResetEmailContent(toName, resetToken, _frontendBaseUrl);

            _logger.LogInformation(
                "PASSWORD RESET EMAIL SENT to {Email}: {Content}",
                toEmail,
                emailContent);

            // Simulate async email sending
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string toName, string organizationName)
    {
        try
        {
            var emailContent = GenerateWelcomeEmailContent(toName, organizationName);

            _logger.LogInformation(
                "WELCOME EMAIL SENT to {Email} for {Organization}: {Content}",
                toEmail,
                organizationName,
                emailContent);

            // Simulate async email sending
            await Task.Delay(100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            return false;
        }
    }

    private static string GenerateInvitationEmailContent(
        string toName,
        string organizationName,
        string invitedByName,
        string invitationToken,
        string? message,
        string frontendBaseUrl)
    {
        return $@"
Subject: You've been invited to join {organizationName}

Dear {toName},

{invitedByName} has invited you to join {organizationName} on our sermon transcription platform.

{(!string.IsNullOrEmpty(message) ? $"Personal message: {message}\n\n" : "")}
To accept this invitation, please click the following link:
{frontendBaseUrl.TrimEnd('/')}/accept-invitation?token={invitationToken}

This invitation will expire in 7 days.

If you have any questions, please contact {invitedByName}.

Best regards,
The Sermon Transcription Team";
    }

    private static string GeneratePasswordResetEmailContent(string toName, string resetToken, string frontendBaseUrl)
    {
        return $@"
Subject: Password Reset Request

Dear {toName},

You have requested to reset your password. Please click the following link to set a new password:

{frontendBaseUrl.TrimEnd('/')}/reset-password?token={resetToken}

This link will expire in 1 hour.

If you did not request this password reset, please ignore this email.

Best regards,
The Sermon Transcription Team";
    }

    private static string GenerateWelcomeEmailContent(string toName, string organizationName)
    {
        return $@"
Subject: Welcome to {organizationName}!

Dear {toName},

Welcome to {organizationName}! Your account has been successfully created and you are now a member of our sermon transcription platform.

You can now:
- Access your organization's transcriptions
- Create new transcription sessions
- Manage your profile and settings

If you have any questions or need assistance, please don't hesitate to contact your organization administrator.

Best regards,
The Sermon Transcription Team";
    }
}
