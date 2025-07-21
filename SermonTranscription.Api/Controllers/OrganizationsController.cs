using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Organization management controller with CRUD operations
/// </summary>
[Route("api/v{version:apiVersion}/organizations")]
[ApiVersion("1.0")]
public class OrganizationsController : BaseApiController
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService, ILogger<OrganizationsController> logger)
        : base(logger)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <returns>Created organization details</returns>
    [HttpPost]
    [RequireAuthenticatedUser]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "User not authenticated",
                    Errors = ["Valid authentication required"]
                });
            }

            var result = await _organizationService.CreateOrganizationAsync(request, userId.Value);
            return HandleServiceResult(result, () => CreatedAtAction(nameof(GetOrganization), new { id = result.Data!.Id }, result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error creating organization");
        }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    [HttpGet("{id:guid}")]
    [RequireOrganizationMember]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        try
        {
            var result = await _organizationService.GetOrganizationAsync(id);
            return HandleServiceResult<OrganizationResponse>(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization {id}");
        }
    }

    /// <summary>
    /// Get organization by slug
    /// </summary>
    /// <param name="slug">Organization slug</param>
    /// <returns>Organization details</returns>
    [HttpGet("slug/{slug}")]
    [RequireOrganizationMember]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationBySlug(string slug)
    {
        try
        {
            var result = await _organizationService.GetOrganizationBySlugAsync(slug);
            return HandleServiceResult<OrganizationResponse>(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization by slug {slug}");
        }
    }

    /// <summary>
    /// Get organizations with search and pagination
    /// </summary>
    /// <param name="request">Search and pagination parameters</param>
    /// <returns>Paginated list of organizations</returns>
    [HttpGet]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizations([FromQuery] OrganizationSearchRequest request)
    {
        try
        {
            var result = await _organizationService.GetOrganizationsAsync(request);
            return HandleServiceResult<OrganizationListResponse>(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error retrieving organizations");
        }
    }

    /// <summary>
    /// Update organization details
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("{id:guid}")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _organizationService.UpdateOrganizationAsync(id, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization {id}");
        }
    }

    /// <summary>
    /// Update organization settings
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Settings update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("{id:guid}/settings")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationSettings(Guid id, [FromBody] UpdateOrganizationSettingsRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _organizationService.UpdateOrganizationSettingsAsync(id, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization settings {id}");
        }
    }

    /// <summary>
    /// Update organization logo
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Logo update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("{id:guid}/logo")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationLogo(Guid id, [FromBody] UpdateOrganizationLogoRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var result = await _organizationService.UpdateOrganizationLogoAsync(id, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization logo {id}");
        }
    }

    /// <summary>
    /// Delete organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id:guid}")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        try
        {
            var result = await _organizationService.DeleteOrganizationAsync(id);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deleting organization {id}");
        }
    }

    /// <summary>
    /// Activate organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Activation confirmation</returns>
    [HttpPost("{id:guid}/activate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateOrganization(Guid id)
    {
        try
        {
            var result = await _organizationService.ActivateOrganizationAsync(id);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error activating organization {id}");
        }
    }

    /// <summary>
    /// Deactivate organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Deactivation confirmation</returns>
    [HttpPost("{id:guid}/deactivate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOrganization(Guid id)
    {
        try
        {
            var result = await _organizationService.DeactivateOrganizationAsync(id);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deactivating organization {id}");
        }
    }

    /// <summary>
    /// Get organization with users
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details with user information</returns>
    [HttpGet("{id:guid}/users")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithUsers(Guid id)
    {
        try
        {
            var result = await _organizationService.GetOrganizationWithUsersAsync(id);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization with users {id}");
        }
    }

    /// <summary>
    /// Get organization with subscriptions
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details with subscription information</returns>
    [HttpGet("{id:guid}/subscriptions")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithSubscriptions(Guid id)
    {
        try
        {
            var result = await _organizationService.GetOrganizationWithSubscriptionsAsync(id);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization with subscriptions {id}");
        }
    }
}
