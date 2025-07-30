namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for user organizations pagination request
/// </summary>
public class UserOrganizationsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}
