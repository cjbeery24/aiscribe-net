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
        var result = await _authService.LoginAsync(request.Email, request.Password);
        return HandleServiceResult(result, () => Ok(result.Data));
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
        var result = await _authService.RegisterAsync(request);
        return HandleServiceResult(result, () => StatusCode(201, SuccessResponse(result.Data, "Registration successful")));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Logout user by revoking refresh token
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

        var userId = HttpContext.GetUserId()!.Value;
        var result = await _authService.RevokeAllUserRefreshTokensAsync(userId);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Request password reset for user
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("forgot-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("reset-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
        return HandleServiceResult(result, () => Ok(result.Data));
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var invitedByUserId = HttpContext.GetUserId()!.Value;

        var result = await _invitationService.InviteUserAsync(request, tenantContext.OrganizationId, invitedByUserId);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    /// <param name="request">Accept invitation request</param>
    /// <returns>Acceptance result</returns>
    [HttpPost("accept-invitation")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var result = await _invitationService.AcceptInvitationAsync(request);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get user's organizations
    /// </summary>
    /// <returns>List of user's organizations</returns>
    [HttpGet("organizations")]
    [Authorize]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(List<OrganizationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserOrganizations()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _authService.GetUserOrganizationsAsync(userId);
        return HandleServiceResult(result, () => Ok(result.Data));
    }
}
