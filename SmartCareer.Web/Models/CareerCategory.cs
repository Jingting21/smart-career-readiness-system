namespace SmartCareer.Web.Models;

public sealed class CareerCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Career> Careers { get; set; } = [];
}
