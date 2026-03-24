namespace Feirb.Shared.Mail;

public record PaginatedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount);
