namespace SmartCareer.Web.Models;

public sealed class Skill
{
    public int SkillId { get; set; }
    public string ExternalSkillCode { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public string SkillCategory { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CareerSkill> CareerSkills { get; set; } = [];
}
