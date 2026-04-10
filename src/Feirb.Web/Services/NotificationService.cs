namespace Feirb.Web.Services;

public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed class NotificationItem(string message, NotificationSeverity severity)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; } = message;
    public NotificationSeverity Severity { get; } = severity;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}

public sealed class NotificationService
{
    private static readonly Dictionary<NotificationSeverity, int?> AutoDismissSeconds = new()
    {
        [NotificationSeverity.Error] = null,
        [NotificationSeverity.Warning] = 30,
        [NotificationSeverity.Success] = 10,
        [NotificationSeverity.Info] = 10,
    };

    private readonly List<NotificationItem> _notifications = [];

    public IReadOnlyList<NotificationItem> Notifications => _notifications;

    public event Action? OnChange;

    public static int? GetAutoDismissSeconds(NotificationSeverity severity) =>
        AutoDismissSeconds[severity];

    public NotificationItem Add(string message, NotificationSeverity severity)
    {
        var item = new NotificationItem(message, severity);
        _notifications.Add(item);
        OnChange?.Invoke();
        return item;
    }

    public void Dismiss(Guid id)
    {
        var item = _notifications.FirstOrDefault(n => n.Id == id);
        if (item is null)
            return;

        _notifications.Remove(item);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _notifications.Clear();
        OnChange?.Invoke();
    }
}
