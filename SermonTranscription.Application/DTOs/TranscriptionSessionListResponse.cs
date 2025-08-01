using SermonTranscription.Domain.Common;

namespace SermonTranscription.Application.DTOs;

public class TranscriptionSessionListResponse : PaginatedResult<TranscriptionSessionResponse>
{
    public TranscriptionSessionListResponse(
        IEnumerable<TranscriptionSessionResponse> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasNextPage = PageNumber < TotalPages;
        HasPreviousPage = PageNumber > 1;
    }
}
