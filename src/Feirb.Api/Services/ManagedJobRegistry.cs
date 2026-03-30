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
            _jobs[registration.JobTypeName] = registration.ClrType;
        }
    }

    public Type GetClrType(string jobTypeName) =>
        _jobs.TryGetValue(jobTypeName, out var type)
            ? type
            : throw new InvalidOperationException($"No managed job registered for '{jobTypeName}'.");

    public bool HasJobType(string jobTypeName) =>
        _jobs.ContainsKey(jobTypeName);
}
