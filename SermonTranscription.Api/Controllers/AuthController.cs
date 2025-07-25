using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Authentication controller for user login, registration, and token management
/// </summary>
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly InvitationService _invitationService;

    public AuthController(IAuthService authService, InvitationService invitationService, ILogger<AuthController> logger)
        : base(logger)
    {
        _authService = authService;
        _invitationService = invitationService;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(new LoginResponse
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                User = result.User!
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error during login for email: {request.Email}");
        }
    }

    /// <summary>
    /// User registration endpoint
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("already exists"))
                {
                    return Conflict(new ErrorResponse
                    {
                        Message = result.Message,
                        Errors = [result.Message]
                    });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return CreatedAtAction(nameof(Register), new RegisterResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error during registration for email: {request.Email}");
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(new RefreshResponse
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error during token refresh");
        }
    }

    /// <summary>
    /// Logout endpoint - revokes all refresh tokens for the current user
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = HttpContext.GetUserId()!.Value;

            await _authService.RevokeAllUserRefreshTokensAsync(userId);

            _logger.LogInformation("User {UserId} logged out and all refresh tokens revoked", userId);

            return Ok(new LogoutResponse
            {
                Message = "Successfully logged out and all sessions terminated"
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Request password reset for a user account
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset request result</returns>
    [HttpPost("forgot-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _authService.ForgotPasswordAsync(request.Email);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(new ForgotPasswordResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error during forgot password for email: {request.Email}");
        }
    }

    /// <summary>
    /// Reset password using a valid reset token
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset result</returns>
    [HttpPost("reset-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(new ResetPasswordResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error during password reset");
        }
    }

    /// <summary>
    /// Invite a user to join an organization
    /// </summary>
    /// <param name="request">Invitation request</param>
    /// <returns>Invitation result</returns>
    [HttpPost("invite")]
    [Authorize]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var tenantContext = HttpContext.GetTenantContext()!;
            var userId = HttpContext.GetUserId()!.Value;

            var result = await _invitationService.InviteUserAsync(request, tenantContext.OrganizationId, userId);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error during user invitation");
        }
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    /// <param name="request">Invitation acceptance request</param>
    /// <returns>Acceptance result with authentication tokens</returns>
    [HttpPost("accept-invitation")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _invitationService.AcceptInvitationAsync(request);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error during invitation acceptance");
        }
    }

    /// <summary>
    /// Get user's available organizations
    /// </summary>
    /// <returns>List of organizations the user is a member of</returns>
    [HttpGet("organizations")]
    [Authorize]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(List<OrganizationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserOrganizations()
    {
        try
        {
            var userId = HttpContext.GetUserId()!.Value;

            var organizations = await _authService.GetUserOrganizationsAsync(userId);

            return Ok(organizations);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error getting organizations for user");
        }
    }
}
