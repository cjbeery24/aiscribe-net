namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization user search request
/// </summary>
public class OrganizationUserSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailVerified { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "FirstName";
    public bool SortDescending { get; set; } = false;
}
