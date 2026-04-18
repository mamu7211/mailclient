namespace Feirb.Shared.Mail;

public record MessageListResponse(
    List<MessageListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int PendingCount,
    bool JobPaused);
