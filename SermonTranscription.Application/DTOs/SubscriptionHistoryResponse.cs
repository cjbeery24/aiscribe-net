namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for subscription history response with pagination
/// </summary>
public class SubscriptionHistoryResponse
{
    public List<SubscriptionResponse> Subscriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
