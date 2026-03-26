namespace Feirb.Api.Data.Entities;

public class WidgetConfig
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required string WidgetInstanceId { get; set; }
    public required string ConfigValue { get; set; }
}
