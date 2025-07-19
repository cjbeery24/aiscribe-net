using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Example controller demonstrating role-based authorization attributes
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    /// <summary>
    /// Endpoint that requires organization admin access
    /// </summary>
    [HttpGet("admin-only")]
    [RequireOrganizationAdmin]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "This endpoint requires organization admin access" });
    }

    /// <summary>
    /// Endpoint that requires ability to manage users
    /// </summary>
    [HttpGet("manage-users")]
    [RequireCanManageUsers]
    public IActionResult ManageUsers()
    {
        return Ok(new { message = "This endpoint requires ability to manage users" });
    }

    /// <summary>
    /// Endpoint that requires ability to manage transcriptions
    /// </summary>
    [HttpGet("manage-transcriptions")]
    [RequireCanManageTranscriptions]
    public IActionResult ManageTranscriptions()
    {
        return Ok(new { message = "This endpoint requires ability to manage transcriptions" });
    }

    /// <summary>
    /// Endpoint that requires ability to view transcriptions
    /// </summary>
    [HttpGet("view-transcriptions")]
    [RequireCanViewTranscriptions]
    public IActionResult ViewTranscriptions()
    {
        return Ok(new { message = "This endpoint requires ability to view transcriptions" });
    }

    /// <summary>
    /// Endpoint that requires ability to export transcriptions
    /// </summary>
    [HttpGet("export-transcriptions")]
    [RequireCanExportTranscriptions]
    public IActionResult ExportTranscriptions()
    {
        return Ok(new { message = "This endpoint requires ability to export transcriptions" });
    }

    /// <summary>
    /// Endpoint that requires organization membership (any active member)
    /// </summary>
    [HttpGet("organization-member")]
    [RequireOrganizationMember]
    public IActionResult OrganizationMember()
    {
        return Ok(new { message = "This endpoint requires organization membership" });
    }

    /// <summary>
    /// Endpoint that requires authentication (any valid JWT token)
    /// </summary>
    [HttpGet("authenticated")]
    [RequireAuthenticatedUser]
    public IActionResult Authenticated()
    {
        return Ok(new { message = "This endpoint requires authentication" });
    }

    /// <summary>
    /// Endpoint that combines multiple authorization requirements
    /// </summary>
    [HttpPost("complex-operation")]
    [RequireOrganizationAdmin]
    public IActionResult ComplexOperation()
    {
        return Ok(new { message = "This endpoint requires organization admin access for complex operations" });
    }
} 