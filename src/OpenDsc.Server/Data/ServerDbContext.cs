// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Data;

/// <summary>
/// Entity Framework Core database context for the pull server.
/// </summary>
public sealed class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Registered nodes.
    /// </summary>
    public DbSet<Node> Nodes => Set<Node>();

    /// <summary>
    /// Stored configurations.
    /// </summary>
    public DbSet<Configuration> Configurations => Set<Configuration>();

    /// <summary>
    /// Configuration versions.
    /// </summary>
    public DbSet<ConfigurationVersion> ConfigurationVersions => Set<ConfigurationVersion>();

    /// <summary>
    /// Configuration files.
    /// </summary>
    public DbSet<ConfigurationFile> ConfigurationFiles => Set<ConfigurationFile>();

    /// <summary>
    /// Parameter scopes.
    /// </summary>
    public DbSet<Scope> Scopes => Set<Scope>();

    /// <summary>
    /// Parameter versions.
    /// </summary>
    public DbSet<ParameterVersion> ParameterVersions => Set<ParameterVersion>();

    /// <summary>
    /// Node configuration assignments.
    /// </summary>
    public DbSet<NodeConfiguration> NodeConfigurations => Set<NodeConfiguration>();

    /// <summary>
    /// Node scope assignments.
    /// </summary>
    public DbSet<NodeScopeAssignment> NodeScopeAssignments => Set<NodeScopeAssignment>();

    /// <summary>
    /// Compliance reports from nodes.
    /// </summary>
    public DbSet<Report> Reports => Set<Report>();

    /// <summary>
    /// Registration keys for node authorization.
    /// </summary>
    public DbSet<RegistrationKey> RegistrationKeys => Set<RegistrationKey>();

    /// <summary>
    /// Server settings (singleton).
    /// </summary>
    public DbSet<ServerSettings> ServerSettings => Set<ServerSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Fqdn).IsUnique();
            entity.HasIndex(e => e.CertificateThumbprint).IsUnique();
            entity.Property(e => e.Fqdn).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ConfigurationName).HasMaxLength(255);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CertificateSubject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EntryPoint).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<ConfigurationVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.Version }).IsUnique();
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PrereleaseChannel).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(255);

            entity.HasOne(e => e.Configuration)
                .WithMany(c => c.Versions)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VersionId, e.RelativePath }).IsUnique();
            entity.Property(e => e.RelativePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();

            entity.HasOne(e => e.Version)
                .WithMany(v => v.Files)
                .HasForeignKey(e => e.VersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Scope>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Precedence);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<ParameterVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScopeId, e.ConfigurationId, e.Version }).IsUnique();
            entity.HasIndex(e => new { e.ScopeId, e.ConfigurationId, e.IsActive });
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(255);

            entity.HasOne(e => e.Scope)
                .WithMany(s => s.ParameterVersions)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Configuration)
                .WithMany(c => c.ParameterVersions)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeConfiguration>(entity =>
        {
            entity.HasKey(e => new { e.NodeId, e.ConfigurationId });
            entity.Property(e => e.PrereleaseChannel).HasMaxLength(50);

            entity.HasOne(e => e.Node)
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Configuration)
                .WithMany(c => c.NodeConfigurations)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ActiveVersion)
                .WithMany(v => v.NodeConfigurations)
                .HasForeignKey(e => e.ActiveVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NodeScopeAssignment>(entity =>
        {
            entity.HasKey(e => new { e.NodeId, e.ScopeId });

            entity.HasOne(e => e.Node)
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Scope)
                .WithMany(s => s.NodeAssignments)
                .HasForeignKey(e => e.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NodeId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Operation).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ResultJson).IsRequired();

            entity.HasOne(e => e.Node)
                .WithMany(n => n.Reports)
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegistrationKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Key).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<ServerSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdminApiKeyHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AdminApiKeySalt).HasMaxLength(64).IsRequired();
        });

        SeedDefaultScopes(modelBuilder);
    }

    private static void SeedDefaultScopes(ModelBuilder modelBuilder)
    {
        var now = DateTimeOffset.UtcNow;

        modelBuilder.Entity<Scope>().HasData(
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Default",
                Description = "Default parameters applied to all configurations",
                Precedence = 0,
                CreatedAt = now
            },
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Role",
                Description = "Role-specific parameter overrides",
                Precedence = 10,
                CreatedAt = now
            },
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Region",
                Description = "Regional parameter overrides",
                Precedence = 20,
                CreatedAt = now
            },
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Name = "Customer",
                Description = "Customer-specific parameter overrides",
                Precedence = 30,
                CreatedAt = now
            },
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                Name = "Environment",
                Description = "Environment-specific parameter overrides (dev/staging/prod)",
                Precedence = 40,
                CreatedAt = now
            },
            new Scope
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                Name = "Node",
                Description = "Node-specific parameter overrides",
                Precedence = 50,
                CreatedAt = now
            }
        );
    }
}
