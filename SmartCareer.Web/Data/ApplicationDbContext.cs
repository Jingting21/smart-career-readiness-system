
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartCareer.Web.Models;

namespace SmartCareer.Web.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public DbSet<CareerCategory> CareerCategories => Set<CareerCategory>();
        public DbSet<Career> Careers => Set<Career>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<CareerSkill> CareerSkills => Set<CareerSkill>();
        public DbSet<CareerCatalogSource> CareerCatalogSources => Set<CareerCatalogSource>();

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CareerCategory>(entity =>
            {
                entity.HasKey(category => category.CategoryId);
                entity.Property(category => category.CategoryName)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(category => category.Slug)
                    .HasColumnType("varchar(100)")
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(category => category.Description)
                    .HasMaxLength(500);
                entity.Property(category => category.DisplayOrder)
                    .HasDefaultValue((short)0);
                entity.Property(category => category.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(category => category.CategoryName)
                    .IsUnique();
                entity.HasIndex(category => category.Slug)
                    .IsUnique();
                entity.HasIndex(category => new { category.IsActive, category.DisplayOrder });
            });

            modelBuilder.Entity<Career>(entity =>
            {
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_Careers_LocalReview",
                    "([RequiresLocalReview] = 0 AND [LocalReviewNote] IS NULL) OR " +
                    "([RequiresLocalReview] = 1 AND [LocalReviewNote] IS NOT NULL AND " +
                    "LEN(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE([LocalReviewNote], CHAR(9), N''), CHAR(10), N''), CHAR(13), N'')))) > 0)"));
                entity.HasKey(career => career.CareerId);
                entity.Property(career => career.OccupationCode)
                    .HasColumnType("varchar(10)")
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(career => career.SourceTitle)
                    .HasColumnType("nvarchar(150)")
                    .HasMaxLength(150)
                    .IsRequired();
                entity.Property(career => career.Title)
                    .HasMaxLength(150)
                    .IsRequired();
                entity.Property(career => career.Description)
                    .HasMaxLength(1000)
                    .IsRequired();
                entity.Property(career => career.Responsibilities)
                    .HasColumnType("nvarchar(max)");
                entity.Property(career => career.Qualifications)
                    .HasColumnType("nvarchar(max)");
                entity.Property(career => career.SourceName)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(career => career.SourceVersion)
                    .HasColumnType("varchar(20)")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(career => career.SourceUpdatedOn)
                    .HasColumnType("date");
                entity.Property(career => career.RequiresLocalReview)
                    .HasColumnType("bit")
                    .HasDefaultValue(false);
                entity.Property(career => career.LocalReviewNote)
                    .HasColumnType("nvarchar(500)")
                    .HasMaxLength(500);
                entity.Property(career => career.IsActive)
                    .HasDefaultValue(true);
                entity.Property(career => career.CreatedAt)
                    .HasColumnType("datetime2");
                entity.Property(career => career.UpdatedAt)
                    .HasColumnType("datetime2");

                entity.HasIndex(career => career.OccupationCode)
                    .IsUnique();
                entity.HasIndex(career => new { career.CategoryId, career.IsActive, career.Title });
                entity.HasIndex(career => new { career.IsActive, career.Title });

                entity.HasOne(career => career.Category)
                    .WithMany(category => category.Careers)
                    .HasForeignKey(career => career.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CareerCatalogSource>(entity =>
            {
                entity.HasKey(source => source.CareerCatalogSourceId);
                entity.Property(source => source.SourceName)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(source => source.SourceVersion)
                    .HasColumnType("varchar(20)")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(source => source.OfficialSourceUrl)
                    .HasColumnType("varchar(500)")
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(source => source.LicenseName)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(source => source.LicenseUrl)
                    .HasColumnType("varchar(500)")
                    .HasMaxLength(500)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(source => source.RetrievedOn)
                    .HasColumnType("date")
                    .IsRequired();
                entity.Property(source => source.AttributionText)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.HasIndex(source => new { source.SourceName, source.SourceVersion })
                    .IsUnique();
            });

            modelBuilder.Entity<Skill>(entity =>
            {
                entity.HasKey(skill => skill.SkillId);
                entity.Property(skill => skill.ExternalSkillCode)
                    .HasColumnType("varchar(20)")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();
                entity.Property(skill => skill.SkillName)
                    .HasMaxLength(150)
                    .IsRequired();
                entity.Property(skill => skill.SkillCategory)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(skill => skill.Description)
                    .HasMaxLength(1000);
                entity.Property(skill => skill.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(skill => skill.ExternalSkillCode)
                    .IsUnique();
                entity.HasIndex(skill => new { skill.SkillCategory, skill.SkillName });
            });

            modelBuilder.Entity<CareerSkill>(entity =>
            {
                entity.HasKey(careerSkill => new { careerSkill.CareerId, careerSkill.SkillId });
                entity.Property(careerSkill => careerSkill.Importance)
                    .HasPrecision(4, 2);
                entity.Property(careerSkill => careerSkill.RequiredLevel)
                    .HasPrecision(4, 2);
                entity.Property(careerSkill => careerSkill.SourceVersion)
                    .HasColumnType("varchar(20)")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();

                entity.HasIndex(careerSkill => new { careerSkill.SkillId, careerSkill.CareerId });

                entity.HasOne(careerSkill => careerSkill.Career)
                    .WithMany(career => career.CareerSkills)
                    .HasForeignKey(careerSkill => careerSkill.CareerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(careerSkill => careerSkill.Skill)
                    .WithMany(skill => skill.CareerSkills)
                    .HasForeignKey(careerSkill => careerSkill.SkillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
