using SermonTranscription.Domain.Common;
using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Application.DTOs;

public class TranscriptionSessionSearchRequest : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public SessionStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsLive { get; set; }
    public string? Language { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public new string SortBy { get; set; } = "CreatedAt";
    public new bool SortDescending { get; set; } = true;
}
