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
    /// Compliance reports from nodes.
    /// </summary>
    public DbSet<Report> Reports => Set<Report>();

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
            entity.Property(e => e.Fqdn).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ConfigurationName).HasMaxLength(255);
            entity.Property(e => e.ApiKeyHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();
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

        modelBuilder.Entity<ServerSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegistrationKey).HasMaxLength(64).IsRequired();
            entity.Property(e => e.AdminApiKeyHash).HasMaxLength(64).IsRequired();
        });
    }
}
