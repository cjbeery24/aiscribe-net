using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
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
    [ProducesResponseType(typeof(ApiSuccessResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Login successful"));
    }

    /// <summary>
    /// User registration endpoint
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => CreatedResponse(result.Data, "Registration successful"));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Token refreshed successfully"));
    }

    /// <summary>
    /// Logout user by revoking refresh token
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(ApiSuccessResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _authService.RevokeAllUserRefreshTokensAsync(userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Logout successful"));
    }

    /// <summary>
    /// Request password reset for user
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("forgot-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Password reset request processed successfully"));
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("reset-password")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Password reset successfully"));
    }

    /// <summary>
    /// Invite a user to join an organization
    /// </summary>
    /// <param name="request">Invitation request</param>
    /// <returns>Invitation result</returns>
    [HttpPost("invite")]
    [Authorize]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(ApiSuccessResponse<InviteUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var invitedByUserId = HttpContext.GetUserId()!.Value;
        var result = await _invitationService.InviteUserAsync(request, tenantContext.OrganizationId, invitedByUserId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "User invitation sent successfully"));
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    /// <param name="request">Accept invitation request</param>
    /// <returns>Acceptance result</returns>
    [HttpPost("accept-invitation")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<AcceptInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var result = await _invitationService.AcceptInvitationAsync(request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Invitation accepted successfully"));
    }

    /// <summary>
    /// Get user's organizations
    /// </summary>
    /// <returns>List of user's organizations</returns>
    [HttpGet("organizations")]
    [Authorize]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<OrganizationSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserOrganizations()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _authService.GetUserOrganizationsAsync(userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "User organizations retrieved successfully"));
    }
}
