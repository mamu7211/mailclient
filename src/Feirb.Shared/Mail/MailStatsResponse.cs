namespace Feirb.Shared.Mail;

public record MailStatsResponse(
    int TotalCount,
    List<MailboxMailStats> MailsPerMailbox,
    List<MailStats> TimeSeries,
    StatsGranularity Granularity);
