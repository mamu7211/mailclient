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

public sealed class NotificationService : IDisposable
{
    private static readonly Dictionary<NotificationSeverity, TimeSpan?> _autoDismissDelays = new()
    {
        [NotificationSeverity.Error] = null,
        [NotificationSeverity.Warning] = TimeSpan.FromSeconds(30),
        [NotificationSeverity.Success] = TimeSpan.FromSeconds(10),
        [NotificationSeverity.Info] = TimeSpan.FromSeconds(10),
    };

    private readonly List<NotificationItem> _notifications = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = [];

    public IReadOnlyList<NotificationItem> Notifications => _notifications;

    public event Action? OnChange;

    public NotificationItem Add(string message, NotificationSeverity severity)
    {
        var item = new NotificationItem(message, severity);
        _notifications.Add(item);

        var delay = _autoDismissDelays[severity];
        if (delay.HasValue)
            ScheduleAutoDismissAsync(item.Id, delay.Value);

        OnChange?.Invoke();
        return item;
    }

    public void Dismiss(Guid id)
    {
        var item = _notifications.FirstOrDefault(n => n.Id == id);
        if (item is null)
            return;

        CancelTimer(id);
        _notifications.Remove(item);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        foreach (var cts in _timers.Values)
            cts.Cancel();

        _timers.Clear();
        _notifications.Clear();
        OnChange?.Invoke();
    }

    public void Dispose()
    {
        foreach (var cts in _timers.Values)
            cts.Cancel();

        _timers.Clear();
    }

    internal void ScheduleAutoDismissAsync(Guid id, TimeSpan delay)
    {
        var cts = new CancellationTokenSource();
        _timers[id] = cts;

        _ = DismissAfterDelayAsync(id, delay, cts.Token);
    }

    private async Task DismissAfterDelayAsync(Guid id, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            Dismiss(id);
        }
        catch (TaskCanceledException)
        {
            // Dismissed manually before timer fired
        }
    }

    private void CancelTimer(Guid id)
    {
        if (_timers.Remove(id, out var cts))
            cts.Cancel();
    }
}
