using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SermonTranscription.Infrastructure.Services;

/// <summary>
/// JWT service implementation for token generation and validation
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateAccessToken(User user, Guid organizationId, string role)
    {
        try
        {
            var tokenHandler = new JsonWebTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Role, role),
                new("organizationId", organizationId.ToString()),
                new("userId", user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
            throw new InvalidOperationException("Failed to generate access token", ex);
        }
    }

    public string GenerateRefreshToken(User user)
    {
        try
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            
            var refreshToken = Convert.ToBase64String(randomNumber);
            
            _logger.LogInformation("Generated refresh token for user {UserId}", user.Id);
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token for user {UserId}", user.Id);
            throw new InvalidOperationException("Failed to generate refresh token", ex);
        }
    }

    public JwtUserInfo? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JsonWebTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };

            var result = tokenHandler.ValidateToken(token, validationParameters);

            if (!result.IsValid || result.ClaimsIdentity == null)
            {
                _logger.LogWarning("Invalid token");
                return null;
            }

            var userIdClaim = result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = result.ClaimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaim = result.ClaimsIdentity.FindFirst(ClaimTypes.Role)?.Value;
            var organizationIdClaim = result.ClaimsIdentity.FindFirst("organizationId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return null;
            }

            if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                _logger.LogWarning("Invalid organization ID in token");
                return null;
            }

            return new JwtUserInfo
            {
                UserId = userId,
                Email = emailClaim ?? string.Empty,
                Role = roleClaim ?? string.Empty,
                OrganizationId = organizationId,
                ExpiresAt = result.SecurityToken.ValidTo
            };
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Token has invalid signature");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JsonWebTokenHandler();
            
            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jsonWebToken = tokenHandler.ReadJsonWebToken(token);
            var userIdClaim = jsonWebToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return null;
        }
    }

    public Guid? GetOrganizationIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JsonWebTokenHandler();
            
            if (!tokenHandler.CanReadToken(token))
            {
                return null;
            }

            var jsonWebToken = tokenHandler.ReadJsonWebToken(token);
            var organizationIdClaim = jsonWebToken.Claims.FirstOrDefault(c => c.Type == "organizationId")?.Value;

            if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                return null;
            }

            return organizationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting organization ID from token");
            return null;
        }
    }
} 