using Microsoft.EntityFrameworkCore;
using DBADash.Web.Shared.Models;

namespace DBADash.Web.Core.Data;

/// <summary>
/// Entity Framework DbContext for DBADash database
/// </summary>
public class DBADashContext : DbContext
{
    public DBADashContext(DbContextOptions<DBADashContext> options) : base(options)
    {
    }

    // DbSets for main entities
    public DbSet<InstanceEntity> Instances { get; set; }
    public DbSet<PerformanceSnapshotEntity> PerformanceSnapshots { get; set; }
    public DbSet<BackupEntity> Backups { get; set; }
    public DbSet<AlertEntity> Alerts { get; set; }
    public DbSet<ConfigurationEntity> Configurations { get; set; }
    public DbSet<DatabaseEntity> Databases { get; set; }
    public DbSet<JobEntity> Jobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Instance entity
        modelBuilder.Entity<InstanceEntity>(entity =>
        {
            entity.HasKey(e => e.InstanceId);
            entity.ToTable("Instances", "dbo");
            entity.Property(e => e.InstanceName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ServerName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Port).HasDefaultValue(1433);
            entity.Property(e => e.IsOnline).HasDefaultValue(true);
            entity.Property(e => e.LastSeen).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure PerformanceSnapshot entity
        modelBuilder.Entity<PerformanceSnapshotEntity>(entity =>
        {
            entity.HasKey(e => e.SnapshotId);
            entity.ToTable("PerformanceSnapshots", "dbo");
            entity.Property(e => e.CpuUsagePercent).HasPrecision(5, 2);
            entity.Property(e => e.MemoryUsagePercent).HasPrecision(5, 2);
            entity.Property(e => e.SnapshotDate).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Backup entity
        modelBuilder.Entity<BackupEntity>(entity =>
        {
            entity.HasKey(e => e.BackupId);
            entity.ToTable("Backups", "dbo");
            entity.Property(e => e.DatabaseName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.BackupType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BackupPath).HasMaxLength(2000);
            entity.Property(e => e.BackupSizeMB).HasPrecision(18, 2);
            entity.Property(e => e.CompressedBackupSizeMB).HasPrecision(18, 2);
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Alert entity
        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.HasKey(e => e.AlertId);
            entity.ToTable("Alerts", "dbo");
            entity.Property(e => e.AlertType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.Severity).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(256);
            entity.Property(e => e.Comment).HasMaxLength(2000);
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Configuration entity
        modelBuilder.Entity<ConfigurationEntity>(entity =>
        {
            entity.HasKey(e => new { e.InstanceId, e.ConfigurationName });
            entity.ToTable("Configurations", "dbo");
            entity.Property(e => e.ConfigurationName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(1000);
            entity.Property(e => e.DefaultValue).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Database entity
        modelBuilder.Entity<DatabaseEntity>(entity =>
        {
            entity.HasKey(e => e.DatabaseId);
            entity.ToTable("Databases", "dbo");
            entity.Property(e => e.DatabaseName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.SizeMB).HasPrecision(18, 2);
            entity.Property(e => e.UsedSpaceMB).HasPrecision(18, 2);
            entity.Property(e => e.FreeSpaceMB).HasPrecision(18, 2);
            entity.Property(e => e.CollationName).HasMaxLength(256);
            entity.Property(e => e.CompatibilityLevel).HasMaxLength(10);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Job entity
        modelBuilder.Entity<JobEntity>(entity =>
        {
            entity.HasKey(e => e.JobId);
            entity.ToTable("Jobs", "dbo");
            entity.Property(e => e.JobName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.JobCategory).HasMaxLength(100);
            entity.Property(e => e.Owner).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.LastRunStatus).HasMaxLength(50);
            entity.Property(e => e.NextRunDate);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne<InstanceEntity>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Add indexes for performance
        modelBuilder.Entity<PerformanceSnapshotEntity>()
            .HasIndex(e => new { e.InstanceId, e.SnapshotDate })
            .HasDatabaseName("IX_PerformanceSnapshots_Instance_Date");

        modelBuilder.Entity<BackupEntity>()
            .HasIndex(e => new { e.InstanceId, e.DatabaseName, e.BackupFinishDate })
            .HasDatabaseName("IX_Backups_Instance_Database_Date");

        modelBuilder.Entity<AlertEntity>()
            .HasIndex(e => new { e.InstanceId, e.IsActive, e.CreatedDate })
            .HasDatabaseName("IX_Alerts_Instance_Active_Date");
    }
}

/// <summary>
/// Entity classes that map to database tables
/// </summary>
public class InstanceEntity
{
    public int InstanceId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string? SqlVersion { get; set; }
    public string? SqlEdition { get; set; }
    public bool IsOnline { get; set; } = true;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ConnectionString { get; set; }
    public string? Environment { get; set; }
    public string? Description { get; set; }
}

public class PerformanceSnapshotEntity
{
    public long SnapshotId { get; set; }
    public int InstanceId { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public decimal CpuUsagePercent { get; set; }
    public decimal MemoryUsagePercent { get; set; }
    public long TotalConnections { get; set; }
    public long UserConnections { get; set; }
    public long BlockedProcesses { get; set; }
    public long WaitingTasks { get; set; }
    public long PageReadsPerSec { get; set; }
    public long PageWritesPerSec { get; set; }
    public long BatchRequestsPerSec { get; set; }
    public long SqlCompilationsPerSec { get; set; }
}

public class BackupEntity
{
    public long BackupId { get; set; }
    public int InstanceId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
    public DateTime BackupStartDate { get; set; }
    public DateTime BackupFinishDate { get; set; }
    public decimal BackupSizeMB { get; set; }
    public decimal? CompressedBackupSizeMB { get; set; }
    public string? BackupPath { get; set; }
    public bool IsEncrypted { get; set; }
    public string? ChecksumValue { get; set; }
}

public class AlertEntity
{
    public long AlertId { get; set; }
    public int InstanceId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedDate { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? Comment { get; set; }
}

public class ConfigurationEntity
{
    public int InstanceId { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class DatabaseEntity
{
    public long DatabaseId { get; set; }
    public int InstanceId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string? State { get; set; }
    public decimal SizeMB { get; set; }
    public decimal UsedSpaceMB { get; set; }
    public decimal FreeSpaceMB { get; set; }
    public string? CollationName { get; set; }
    public string? CompatibilityLevel { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class JobEntity
{
    public long JobId { get; set; }
    public int InstanceId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? JobCategory { get; set; }
    public string? Owner { get; set; }
    public string? Description { get; set; }
    public DateTime? LastRunDate { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime? NextRunDate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
