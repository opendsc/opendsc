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
    /// Composite configurations.
    /// </summary>
    public DbSet<CompositeConfiguration> CompositeConfigurations => Set<CompositeConfiguration>();

    /// <summary>
    /// Composite configuration versions.
    /// </summary>
    public DbSet<CompositeConfigurationVersion> CompositeConfigurationVersions => Set<CompositeConfigurationVersion>();

    /// <summary>
    /// Composite configuration items (children).
    /// </summary>
    public DbSet<CompositeConfigurationItem> CompositeConfigurationItems => Set<CompositeConfigurationItem>();

    /// <summary>
    /// Parameter schemas.
    /// </summary>
    public DbSet<ParameterSchema> ParameterSchemas => Set<ParameterSchema>();

    /// <summary>
    /// Scope types for parameter layering.
    /// </summary>
    public DbSet<ScopeType> ScopeTypes => Set<ScopeType>();

    /// <summary>
    /// Scope values within scope types.
    /// </summary>
    public DbSet<ScopeValue> ScopeValues => Set<ScopeValue>();

    /// <summary>
    /// Node tags assigning scope values to nodes.
    /// </summary>
    public DbSet<NodeTag> NodeTags => Set<NodeTag>();

    /// <summary>
    /// Parameter files for configurations.
    /// </summary>
    public DbSet<ParameterFile> ParameterFiles => Set<ParameterFile>();

    /// <summary>
    /// Node configuration assignments.
    /// </summary>
    public DbSet<NodeConfiguration> NodeConfigurations => Set<NodeConfiguration>();

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

    /// <summary>
    /// Users and service accounts.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Roles with permissions.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Internal groups.
    /// </summary>
    public DbSet<Group> Groups => Set<Group>();

    /// <summary>
    /// External group mappings.
    /// </summary>
    public DbSet<ExternalGroupMapping> ExternalGroupMappings => Set<ExternalGroupMapping>();

    /// <summary>
    /// User-role associations.
    /// </summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>
    /// User-group associations.
    /// </summary>
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    /// <summary>
    /// Group-role associations.
    /// </summary>
    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();

    /// <summary>
    /// Personal access tokens.
    /// </summary>
    public DbSet<PersonalAccessToken> PersonalAccessTokens => Set<PersonalAccessToken>();

    /// <summary>
    /// External login providers.
    /// </summary>
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    /// <summary>
    /// Configuration permissions.
    /// </summary>
    public DbSet<ConfigurationPermission> ConfigurationPermissions => Set<ConfigurationPermission>();

    /// <summary>
    /// Composite configuration permissions.
    /// </summary>
    public DbSet<CompositeConfigurationPermission> CompositeConfigurationPermissions => Set<CompositeConfigurationPermission>();

    /// <summary>
    /// Parameter permissions.
    /// </summary>
    public DbSet<ParameterPermission> ParameterPermissions => Set<ParameterPermission>();

    /// <summary>
    /// Audit log.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        modelBuilder.Entity<ScopeType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Precedence).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<ScopeValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScopeTypeId, e.Value }).IsUnique();
            entity.Property(e => e.Value).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.ScopeType)
                .WithMany(st => st.ScopeValues)
                .HasForeignKey(e => e.ScopeTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeTag>(entity =>
        {
            entity.HasKey(e => new { e.NodeId, e.ScopeValueId });

            entity.HasOne(e => e.Node)
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ScopeValue)
                .WithMany(sv => sv.NodeTags)
                .HasForeignKey(e => e.ScopeValueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParameterFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ParameterSchemaId, e.ScopeTypeId, e.ScopeValue, e.Version }).IsUnique();
            entity.HasIndex(e => new { e.ParameterSchemaId, e.ScopeTypeId, e.ScopeValue, e.IsActive });
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ScopeValue).HasMaxLength(255);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Checksum).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(255);

            entity.HasOne(e => e.ScopeType)
                .WithMany(st => st.ParameterFiles)
                .HasForeignKey(e => e.ScopeTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParameterSchema)
                .WithMany(ps => ps.ParameterFiles)
                .HasForeignKey(e => e.ParameterSchemaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeConfiguration>(entity =>
        {
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.PrereleaseChannel).HasMaxLength(50);

            entity.HasOne(e => e.Node)
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Configuration)
                .WithMany(c => c.NodeConfigurations)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CompositeConfiguration)
                .WithMany(c => c.NodeConfigurations)
                .HasForeignKey(e => e.CompositeConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompositeConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EntryPoint).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<CompositeConfigurationVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompositeConfigurationId, e.Version }).IsUnique();
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PrereleaseChannel).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(255);

            entity.HasOne(e => e.CompositeConfiguration)
                .WithMany(c => c.Versions)
                .HasForeignKey(e => e.CompositeConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompositeConfigurationItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompositeConfigurationVersionId, e.Order }).IsUnique();

            entity.HasOne(e => e.CompositeConfigurationVersion)
                .WithMany(v => v.Items)
                .HasForeignKey(e => e.CompositeConfigurationVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChildConfiguration)
                .WithMany()
                .HasForeignKey(e => e.ChildConfigurationId)
                .OnDelete(DeleteBehavior.Restrict);
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
        });

        modelBuilder.Entity<ParameterSchema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.SchemaHash });
            entity.Property(e => e.SchemaHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SchemaDefinition).IsRequired();

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ValidationSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DefaultParameterValidation).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<ConfigurationSettings>(entity =>
        {
            entity.HasKey(e => e.ConfigurationId);
            entity.Property(e => e.ParameterValidation).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.UpdatedBy).HasMaxLength(255);

            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User management entities
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(128);
            entity.Property(e => e.PasswordSalt).HasMaxLength(64);
            entity.Property(e => e.AccountType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Permissions).IsRequired();
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<ExternalGroupMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Provider, e.ExternalGroupId }).IsUnique();
            entity.Property(e => e.ExternalGroupId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ExternalGroupName).HasMaxLength(256);
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();

            entity.HasOne<Group>()
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GroupId });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Group>()
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupRole>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.RoleId });

            entity.HasOne<Group>()
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalAccessToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TokenPrefix).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Scopes).IsRequired();
            entity.Property(e => e.LastUsedIpAddress).HasMaxLength(45);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderKey).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ProviderDisplayName).HasMaxLength(100);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.PrincipalType, e.PrincipalId }).IsUnique();
            entity.Property(e => e.PrincipalType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.PermissionLevel).HasConversion<string>().HasMaxLength(20);

            entity.HasOne<Configuration>()
                .WithMany()
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompositeConfigurationPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompositeConfigurationId, e.PrincipalType, e.PrincipalId }).IsUnique();
            entity.Property(e => e.PrincipalType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.PermissionLevel).HasConversion<string>().HasMaxLength(20);

            entity.HasOne<CompositeConfiguration>()
                .WithMany()
                .HasForeignKey(e => e.CompositeConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParameterPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ParameterId, e.PrincipalType, e.PrincipalId }).IsUnique();
            entity.Property(e => e.PrincipalType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.PermissionLevel).HasConversion<string>().HasMaxLength(20);

            entity.HasOne<ParameterSchema>()
                .WithMany()
                .HasForeignKey(e => e.ParameterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
        });

        SeedDefaultScopeTypes(modelBuilder);
        SeedDefaultValidationSettings(modelBuilder);
    }

    private static void SeedDefaultValidationSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValidationSettings>().HasData(
            new ValidationSettings
            {
                Id = new Guid("00000000-0000-0000-0000-000000000001"),
                EnforceSemverCompliance = true,
                DefaultParameterValidation = ParameterValidationMode.Strict,
                AutoCopyParametersOnMinor = true,
                AutoCopyParametersOnMajor = true,
                AllowPreReleaseVersions = true,
                RequireApprovalForPublish = false,
                AllowSemverComplianceOverride = true,
                AllowParameterValidationOverride = true,
                AllowAutoCopyOverride = true
            }
        );
    }

    private static void SeedDefaultScopeTypes(ModelBuilder modelBuilder)
    {
        var now = DateTimeOffset.UtcNow;

        modelBuilder.Entity<ScopeType>().HasData(
            new ScopeType
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Default",
                Description = "Default parameters applied to all configurations",
                Precedence = 0,
                IsSystem = true,
                AllowsValues = false,
                CreatedAt = now
            },
            new ScopeType
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Node",
                Description = "Node-specific parameter overrides matched by FQDN",
                Precedence = 1,
                IsSystem = true,
                AllowsValues = true,
                CreatedAt = now
            }
        );
    }
}
