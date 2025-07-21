namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization search request
/// </summary>
public class OrganizationSearchRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool? HasActiveSubscription { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}
