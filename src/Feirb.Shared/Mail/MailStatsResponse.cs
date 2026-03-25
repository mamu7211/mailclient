namespace Feirb.Shared.Mail;

public record MailStatsResponse(int TotalCount, List<DailyMailCountItem> MailsPerDay);
