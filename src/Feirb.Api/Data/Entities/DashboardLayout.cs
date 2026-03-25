namespace Feirb.Api.Data.Entities;

public class DashboardLayout
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string LayoutJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
