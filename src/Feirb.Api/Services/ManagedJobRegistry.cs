namespace Feirb.Api.Services;

public class ManagedJobRegistry
{
    private readonly Dictionary<string, Type> _jobs;

    public ManagedJobRegistry(IEnumerable<IManagedJobRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        _jobs = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in registrations)
        {
            _jobs[registration.JobName] = registration.JobType;
        }
    }

    public Type GetJobType(string jobName) =>
        _jobs.TryGetValue(jobName, out var type)
            ? type
            : throw new InvalidOperationException($"No managed job registered for '{jobName}'.");

    public bool HasJob(string jobName) =>
        _jobs.ContainsKey(jobName);
}
