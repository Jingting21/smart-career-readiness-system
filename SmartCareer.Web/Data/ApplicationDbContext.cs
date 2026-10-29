
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
        public DbSet<CareerTask> CareerTasks => Set<CareerTask>();
        public DbSet<CareerEducationRating> CareerEducationRatings => Set<CareerEducationRating>();
        public DbSet<CareerTrainingRating> CareerTrainingRatings => Set<CareerTrainingRating>();
        public DbSet<JobZoneDefinition> JobZoneDefinitions => Set<JobZoneDefinition>();
        public DbSet<CareerJobZone> CareerJobZones => Set<CareerJobZone>();
        public DbSet<CareerCatalogSource> CareerCatalogSources => Set<CareerCatalogSource>();
        public DbSet<DegreeField> DegreeFields => Set<DegreeField>();
        public DbSet<CareerDegreeField> CareerDegreeFields => Set<CareerDegreeField>();
        public DbSet<RelatedFieldDatasetSource> RelatedFieldDatasetSources => Set<RelatedFieldDatasetSource>();
        public DbSet<CareerDegreeFieldReviewEvent> CareerDegreeFieldReviewEvents => Set<CareerDegreeFieldReviewEvent>();

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

            modelBuilder.Entity<DegreeField>(entity =>
            {
                entity.HasKey(field => field.DegreeFieldId);
                entity.Property(field => field.CanonicalName).HasMaxLength(150).IsRequired();
                entity.Property(field => field.ClassificationSystem).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(field => field.ClassificationCode).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(field => field.ClassificationVersion).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(field => field.Description).HasMaxLength(1000);
                entity.Property(field => field.IsActive).HasDefaultValue(true);
                entity.HasIndex(field => new { field.ClassificationSystem, field.ClassificationCode, field.ClassificationVersion }).IsUnique();
            });

            modelBuilder.Entity<RelatedFieldDatasetSource>(entity =>
            {
                entity.HasKey(source => source.RelatedFieldDatasetSourceId);
                entity.Property(source => source.DatasetVersion).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(source => source.MappingFileName).HasMaxLength(200).IsRequired();
                entity.Property(source => source.RetrievedOn).HasColumnType("date");
                entity.Property(source => source.SourceMetadataJson).HasColumnType("nvarchar(max)").IsRequired();
                entity.HasIndex(source => source.DatasetVersion).IsUnique();
            });

            modelBuilder.Entity<CareerDegreeField>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_CareerDegreeFields_RelationshipType", "[RelationshipType] IN ('Primary', 'Related', 'Alternative', 'Specialised')");
                    table.HasCheckConstraint("CK_CareerDegreeFields_ConfidenceLevel", "[ConfidenceLevel] IN ('High', 'Medium', 'Low')");
                    table.HasCheckConstraint("CK_CareerDegreeFields_ReviewStatus", "[ReviewStatus] IN ('Draft', 'In Review', 'Approved', 'Rejected', 'Needs Revalidation')");
                    table.HasCheckConstraint("CK_CareerDegreeFields_Publication", "([ReviewStatus] = 'Approved' AND [ReviewedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL) OR ([ReviewStatus] <> 'Approved' AND [IsPublished] = 0 AND [ApprovedAt] IS NULL)");
                });
                entity.HasKey(mapping => mapping.CareerDegreeFieldId);
                entity.Property(mapping => mapping.RelationshipType).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.ConfidenceLevel).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.ReviewStatus).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.EvidenceSummary).HasMaxLength(2000).IsRequired();
                entity.Property(mapping => mapping.OriginalSocCode).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.OriginalCipCode).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.MalaysianNecCode).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
                entity.Property(mapping => mapping.SourceUrls).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(mapping => mapping.SourceVersions).HasMaxLength(1000).IsRequired();
                entity.Property(mapping => mapping.ReviewDate).HasColumnType("date");
                entity.Property(mapping => mapping.ReviewedBy).HasMaxLength(200);
                entity.Property(mapping => mapping.ApprovedAt).HasColumnType("datetime2");
                entity.Property(mapping => mapping.ReviewerNotes).HasMaxLength(2000).IsRequired();
                entity.Property(mapping => mapping.RevalidationDueOn).HasColumnType("date");
                entity.Property(mapping => mapping.IsPublished).HasDefaultValue(false);
                entity.Property(mapping => mapping.ReviewRevision).HasDefaultValue(0).IsConcurrencyToken();
                entity.HasIndex(mapping => new { mapping.CareerId, mapping.DegreeFieldId }).IsUnique();
                entity.HasIndex(mapping => new { mapping.ReviewStatus, mapping.IsPublished });
                entity.HasOne(mapping => mapping.Career).WithMany(career => career.CareerDegreeFields).HasForeignKey(mapping => mapping.CareerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(mapping => mapping.DegreeField).WithMany(field => field.CareerDegreeFields).HasForeignKey(mapping => mapping.DegreeFieldId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(mapping => mapping.DatasetSource).WithMany(source => source.CareerDegreeFields).HasForeignKey(mapping => mapping.RelatedFieldDatasetSourceId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CareerDegreeFieldReviewEvent>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_CareerDegreeFieldReviewEvents_EventType", "[EventType] IN ('ReviewSubmitted', 'Approved', 'Published', 'Unpublished')");
                    table.HasCheckConstraint("CK_CareerDegreeFieldReviewEvents_Decision", "[Decision] IS NULL OR [Decision] IN ('Approve', 'Reject', 'Needs Revalidation')");
                });
                entity.HasKey(item => item.CareerDegreeFieldReviewEventId);
                entity.Property(item => item.ActorUserId).HasMaxLength(450).IsRequired();
                entity.Property(item => item.EventType).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(item => item.Decision).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false);
                entity.Property(item => item.Rationale).HasMaxLength(2000).IsRequired();
                entity.Property(item => item.EvidenceFingerprint).HasColumnType("char(64)").HasMaxLength(64).IsUnicode(false).IsRequired();
                entity.Property(item => item.PreviousReviewStatus).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(item => item.NewReviewStatus).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(item => item.OccurredAt).HasColumnType("datetime2");
                entity.HasIndex(item => new { item.CareerDegreeFieldId, item.OccurredAt });
                entity.HasIndex(item => new { item.CareerDegreeFieldId, item.EventType, item.EvidenceFingerprint });
                entity.HasOne(item => item.CareerDegreeField).WithMany(mapping => mapping.ReviewEvents)
                    .HasForeignKey(item => item.CareerDegreeFieldId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.ActorUser).WithMany()
                    .HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<CareerTask>(entity =>
            {
                entity.HasKey(task => task.CareerTaskId);
                entity.Property(task => task.Statement).HasMaxLength(1000).IsRequired();
                entity.Property(task => task.TaskType).HasMaxLength(20);
                entity.Property(task => task.Importance).HasPrecision(5, 2);
                entity.Property(task => task.Relevance).HasPrecision(5, 2);
                entity.Property(task => task.SourceUpdatedOn).HasColumnType("date");
                entity.Property(task => task.DomainSource).HasMaxLength(50).IsRequired();
                entity.Property(task => task.SourceVersion)
                    .HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.HasIndex(task => new { task.CareerId, task.OnetTaskId }).IsUnique();
                entity.HasIndex(task => new { task.CareerId, task.TaskType, task.Importance });
                entity.HasOne(task => task.Career).WithMany(career => career.CareerTasks)
                    .HasForeignKey(task => task.CareerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CareerEducationRating>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_CareerEducationRatings_ScaleId", "[ScaleId] IN ('RL', 'RQ')");
                    table.HasCheckConstraint("CK_CareerEducationRatings_Category", "[Category] > 0");
                    table.HasCheckConstraint("CK_CareerEducationRatings_Percentage", "[Percentage] IS NULL OR ([Percentage] >= 0 AND [Percentage] <= 100)");
                    table.HasCheckConstraint("CK_CareerEducationRatings_RespondentCount", "[RespondentCount] IS NULL OR [RespondentCount] >= 0");
                });
                entity.HasKey(rating => rating.CareerEducationRatingId);
                ConfigureRating(entity);
                entity.HasIndex(rating => new { rating.CareerId, rating.ElementId, rating.ScaleId, rating.Category }).IsUnique();
                entity.HasIndex(rating => new { rating.CareerId, rating.ScaleId, rating.Percentage });
                entity.HasOne(rating => rating.Career).WithMany(career => career.CareerEducationRatings)
                    .HasForeignKey(rating => rating.CareerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CareerTrainingRating>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_CareerTrainingRatings_ScaleId", "[ScaleId] IN ('RW', 'PT', 'OJ')");
                    table.HasCheckConstraint("CK_CareerTrainingRatings_Category", "[Category] > 0");
                    table.HasCheckConstraint("CK_CareerTrainingRatings_Percentage", "[Percentage] IS NULL OR ([Percentage] >= 0 AND [Percentage] <= 100)");
                    table.HasCheckConstraint("CK_CareerTrainingRatings_RespondentCount", "[RespondentCount] IS NULL OR [RespondentCount] >= 0");
                });
                entity.HasKey(rating => rating.CareerTrainingRatingId);
                ConfigureRating(entity);
                entity.HasIndex(rating => new { rating.CareerId, rating.ElementId, rating.ScaleId, rating.Category }).IsUnique();
                entity.HasIndex(rating => new { rating.CareerId, rating.ElementId, rating.ScaleId, rating.Percentage });
                entity.HasOne(rating => rating.Career).WithMany(career => career.CareerTrainingRatings)
                    .HasForeignKey(rating => rating.CareerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JobZoneDefinition>(entity =>
            {
                entity.ToTable(table => table.HasCheckConstraint("CK_JobZoneDefinitions_JobZone", "[JobZone] BETWEEN 1 AND 5"));
                entity.HasKey(definition => definition.JobZone);
                entity.Property(definition => definition.JobZone).ValueGeneratedNever();
                entity.Property(definition => definition.Name).HasMaxLength(100).IsRequired();
                entity.Property(definition => definition.Experience).HasMaxLength(1000).IsRequired();
                entity.Property(definition => definition.Education).HasMaxLength(1000).IsRequired();
                entity.Property(definition => definition.JobTraining).HasMaxLength(1000).IsRequired();
                entity.Property(definition => definition.Examples).HasMaxLength(1000).IsRequired();
                entity.Property(definition => definition.SvpRange).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false).IsRequired();
                entity.Property(definition => definition.SourceVersion).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
            });

            modelBuilder.Entity<CareerJobZone>(entity =>
            {
                entity.HasKey(assignment => assignment.CareerJobZoneId);
                entity.Property(assignment => assignment.SourceUpdatedOn).HasColumnType("date");
                entity.Property(assignment => assignment.DomainSource).HasMaxLength(50).IsRequired();
                entity.Property(assignment => assignment.SourceVersion).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.HasIndex(assignment => assignment.CareerId).IsUnique();
                entity.HasIndex(assignment => new { assignment.JobZone, assignment.CareerId });
                entity.HasOne(assignment => assignment.Career).WithOne(career => career.CareerJobZone)
                    .HasForeignKey<CareerJobZone>(assignment => assignment.CareerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(assignment => assignment.JobZoneDefinition).WithMany(definition => definition.CareerJobZones)
                    .HasForeignKey(assignment => assignment.JobZone).OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureRating<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
            where TEntity : class
        {
            entity.Property("ElementId").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property("ScaleId").HasColumnType("varchar(3)").HasMaxLength(3).IsUnicode(false).IsRequired();
            entity.Property("CategoryDescription").HasMaxLength(500).IsRequired();
            entity.Property("Percentage").HasPrecision(6, 2);
            entity.Property("StandardError").HasPrecision(10, 4);
            entity.Property("LowerConfidenceBound").HasPrecision(10, 4);
            entity.Property("UpperConfidenceBound").HasPrecision(10, 4);
            entity.Property("SourceUpdatedOn").HasColumnType("date");
            entity.Property("DomainSource").HasMaxLength(50).IsRequired();
            entity.Property("SourceVersion").HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            RejectAuditMutations();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            RejectAuditMutations();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void RejectAuditMutations()
        {
            if (ChangeTracker.Entries<CareerDegreeFieldReviewEvent>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Related-field review audit events are append-only.");
        }
    }
}
