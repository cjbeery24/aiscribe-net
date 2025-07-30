namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for user organizations response with pagination
/// </summary>
public class UserOrganizationsResponse
{
    public List<OrganizationSummaryDto> Organizations { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
