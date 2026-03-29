namespace Feirb.Api.Data.Entities;

public class Avatar
{
    public Guid Id { get; set; }
    public required string EmailHash { get; set; }
    public required string Email { get; set; }
    public required byte[] ImageData { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
