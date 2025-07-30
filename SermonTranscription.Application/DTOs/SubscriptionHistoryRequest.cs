namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for subscription history pagination request
/// </summary>
public class SubscriptionHistoryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public string? Status { get; set; }
    public string? Plan { get; set; }
}
