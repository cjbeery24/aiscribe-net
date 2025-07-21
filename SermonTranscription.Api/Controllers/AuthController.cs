using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Authentication controller for user login, registration, and token management
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
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
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred during login",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// User registration endpoint
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("already exists"))
                {
                    return Conflict(new ErrorResponse
                    {
                        Message = result.Message,
                        Errors = new[] { result.Message }
                    });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
                });
            }

            return CreatedAtAction(nameof(Register), new RegisterResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred during registration",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
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
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred during token refresh",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Validate access token
    /// </summary>
    /// <returns>User information if token is valid</returns>
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "No token provided",
                    Errors = new[] { "Authorization header is required" }
                });
            }

            var result = await _authService.ValidateTokenAsync(token);

            if (!result.IsSuccess)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
                });
            }

            return Ok(result.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred during token validation",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Logout endpoint (client-side token invalidation)
    /// </summary>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // In a real implementation, you might want to blacklist the token
        // For now, we'll just return a success response
        return Ok(new LogoutResponse
        {
            Message = "Successfully logged out"
        });
    }

    /// <summary>
    /// Request password reset for a user account
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset request result</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var result = await _authService.ForgotPasswordAsync(request.Email);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
                });
            }

            return Ok(new ForgotPasswordResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email: {Email}", request.Email);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while processing your request",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Reset password using a valid reset token
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset result</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);

            if (!result.IsSuccess)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = new[] { result.Message }
                });
            }

            return Ok(new ResetPasswordResponse
            {
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while resetting your password",
                Errors = new[] { "Internal server error" }
            });
        }
    }
}
