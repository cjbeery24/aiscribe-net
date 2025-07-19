using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Services;

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
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response model
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public AuthUserInfo User { get; set; } = new();
}

/// <summary>
/// Register response model
/// </summary>
public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Refresh token response model
/// </summary>
public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Logout response model
/// </summary>
public class LogoutResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string[] Errors { get; set; } = Array.Empty<string>();
} 