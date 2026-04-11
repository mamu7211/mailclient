namespace Feirb.Api.Data.Entities;

public class Address
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public bool IsUnknown { get; set; } = true;
    public bool IsBlocked { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public int SeenCount { get; set; }
}
