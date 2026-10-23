namespace SmartCareer.Web.Models;

public sealed class CareerSkill
{
    public int CareerId { get; set; }
    public int SkillId { get; set; }
    public decimal Importance { get; set; }
    public decimal? RequiredLevel { get; set; }
    public string SourceVersion { get; set; } = string.Empty;

    public Career Career { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
