using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Persistence.Entities;

namespace Vacancy.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SearchPresetEntity> SearchPresets => Set<SearchPresetEntity>();
    public DbSet<ParseJobEntity> ParseJobs => Set<ParseJobEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();
    public DbSet<VacancyEntity> Vacancies => Set<VacancyEntity>();
    public DbSet<ParseJobVacancyEntity> ParseJobVacancies => Set<ParseJobVacancyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SearchPresetEntity>(entity =>
        {
            entity.ToTable("search_presets");
            entity.HasKey(preset => preset.Id);
            entity.Property(preset => preset.Platform).HasMaxLength(32).IsRequired();
            entity.Property(preset => preset.Name).HasMaxLength(200).IsRequired();
            entity.Property(preset => preset.Query).HasMaxLength(500).IsRequired();
            entity.Property(preset => preset.SettingsJson).HasColumnType("jsonb");
            entity.Property(preset => preset.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(preset => preset.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(preset => new { preset.Platform, preset.Name });
        });

        modelBuilder.Entity<ParseJobEntity>(entity =>
        {
            entity.ToTable("parse_jobs");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Platform).HasMaxLength(32).IsRequired();
            entity.Property(job => job.PresetName).HasMaxLength(200).IsRequired();
            entity.Property(job => job.SearchQuery).HasMaxLength(500).IsRequired();
            entity.Property(job => job.ExclusionsJson).HasColumnType("jsonb");
            entity.Property(job => job.Status).HasMaxLength(32).IsRequired();
            entity.Property(job => job.FailureReason).HasMaxLength(2000);
            entity.Property(job => job.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(job => job.QueuedAt).HasColumnType("timestamp with time zone");
            entity.Property(job => job.CompletedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
            entity.HasOne(job => job.Preset)
                .WithMany(preset => preset.Jobs)
                .HasForeignKey(job => job.PresetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Type).HasMaxLength(100).IsRequired();
            entity.Property(message => message.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(message => message.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(message => message.AvailableAt).HasColumnType("timestamp with time zone");
            entity.Property(message => message.ProcessedAt).HasColumnType("timestamp with time zone");
            entity.Property(message => message.LastError).HasMaxLength(2000);
            entity.HasIndex(message => new { message.ProcessedAt, message.AvailableAt, message.CreatedAt });
            entity.HasOne(message => message.Job)
                .WithOne(job => job.OutboxMessage)
                .HasForeignKey<OutboxMessageEntity>(message => message.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyEntity>(entity =>
        {
            entity.ToTable("vacancies");
            entity.HasKey(vacancy => vacancy.Id);
            entity.Property(vacancy => vacancy.Platform).HasMaxLength(32).IsRequired();
            entity.Property(vacancy => vacancy.ExternalId).HasMaxLength(200).IsRequired();
            entity.Property(vacancy => vacancy.Title).HasMaxLength(500).IsRequired();
            entity.Property(vacancy => vacancy.Company).HasMaxLength(500);
            entity.Property(vacancy => vacancy.Location).HasMaxLength(500);
            entity.Property(vacancy => vacancy.Url).IsRequired();
            entity.Property(vacancy => vacancy.SalaryFrom).HasPrecision(14, 2);
            entity.Property(vacancy => vacancy.SalaryTo).HasPrecision(14, 2);
            entity.Property(vacancy => vacancy.SalaryCurrency).HasMaxLength(16);
            entity.Property(vacancy => vacancy.Description);
            entity.Property(vacancy => vacancy.RawDataJson).HasColumnType("jsonb");
            entity.Property(vacancy => vacancy.PublishedAt).HasColumnType("timestamp with time zone");
            entity.Property(vacancy => vacancy.FirstSeenAt).HasColumnType("timestamp with time zone");
            entity.Property(vacancy => vacancy.LastSeenAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(vacancy => new { vacancy.Platform, vacancy.ExternalId }).IsUnique();
            entity.HasIndex(vacancy => new { vacancy.Platform, vacancy.PublishedAt });
            entity.HasIndex(vacancy => vacancy.Company);
        });

        modelBuilder.Entity<ParseJobVacancyEntity>(entity =>
        {
            entity.ToTable("parse_job_vacancies");
            entity.HasKey(link => new { link.JobId, link.VacancyId });
            entity.Property(link => link.FoundAt).HasColumnType("timestamp with time zone");
            entity.HasOne(link => link.Job)
                .WithMany(job => job.JobVacancies)
                .HasForeignKey(link => link.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Vacancy)
                .WithMany(vacancy => vacancy.ParseJobs)
                .HasForeignKey(link => link.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
