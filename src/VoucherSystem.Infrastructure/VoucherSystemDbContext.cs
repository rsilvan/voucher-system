using Microsoft.EntityFrameworkCore;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public class VoucherSystemDbContext : DbContext
{
    public VoucherSystemDbContext(DbContextOptions<VoucherSystemDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<InvitationProjectAccess> InvitationProjectAccesses => Set<InvitationProjectAccess>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProjectAccess> ProjectAccesses => Set<ProjectAccess>();
    public DbSet<UsageQuota> UsageQuotas => Set<UsageQuota>();
    public DbSet<ProjectPromotionJob> ProjectPromotionJobs => Set<ProjectPromotionJob>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<GeoLocation> GeoLocations => Set<GeoLocation>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AreaStore> AreaStores => Set<AreaStore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Country).HasMaxLength(2).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Status);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.NormalizedEmail).IsUnique();
            e.HasIndex(x => x.Status);
        });

        // OrganizationMember
        modelBuilder.Entity<OrganizationMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
            e.HasIndex(x => x.RoleId);
        });

        // Project
        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Environment).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.TimeZone).HasMaxLength(64).IsRequired();
            e.Property(x => x.Locale).HasMaxLength(10).IsRequired().HasDefaultValue("pt-BR");
            e.Property(x => x.Country).HasMaxLength(2).IsRequired().HasDefaultValue("BR");
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.IsPrimary).HasDefaultValue(false);
            e.Property(x => x.IsInUse).HasDefaultValue(false);
            e.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.IsPrimary);
        });

        // Role
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Scope).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });

        // Permission
        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Resource).HasMaxLength(50).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });

        // RolePermission (composite key)
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
        });

        // Plan
        modelBuilder.Entity<Plan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        // PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        // Invitation
        modelBuilder.Entity<Invitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.NormalizedEmail, x.Status });
        });

        // InvitationProjectAccess (composite key)
        modelBuilder.Entity<InvitationProjectAccess>(e =>
        {
            e.HasKey(x => new { x.InvitationId, x.ProjectId });
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.ResourceType).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
            e.HasIndex(x => new { x.OrganizationId, x.Action });
        });

        // ProjectAccess
        modelBuilder.Entity<ProjectAccess>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationMemberId, x.ProjectId }).IsUnique();
            e.HasIndex(x => x.ProjectId);
        });

        // UsageQuota
        modelBuilder.Entity<UsageQuota>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrganizationId);
        });

        // ProjectPromotionJob
        modelBuilder.Entity<ProjectPromotionJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(200);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.HasIndex(x => x.Status);
        });

        // BrandProfile
        modelBuilder.Entity<BrandProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.WebsiteUrl).HasMaxLength(2048);
            e.Property(x => x.TermsUrl).HasMaxLength(2048);
            e.Property(x => x.PrivacyUrl).HasMaxLength(2048);
            e.Property(x => x.SupportEmail).HasMaxLength(254);
            e.Property(x => x.LogoUrl).HasMaxLength(2048);
            e.Property(x => x.PrimaryColor).HasMaxLength(7);
            e.Property(x => x.SecondaryColor).HasMaxLength(7);

            e.OwnsOne(x => x.Address, a =>
            {
                a.Property(p => p.Street).HasColumnName("Address_Street").HasMaxLength(500);
                a.Property(p => p.City).HasColumnName("Address_City").HasMaxLength(200);
                a.Property(p => p.State).HasColumnName("Address_State").HasMaxLength(100);
                a.Property(p => p.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(20);
                a.Property(p => p.Country).HasColumnName("Address_Country").HasMaxLength(2);
            });

            e.HasIndex(x => x.ProjectId).IsUnique();
        });

        // GeoLocation
        modelBuilder.Entity<GeoLocation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Coordinates).HasColumnType("text").IsRequired();
            e.Property(x => x.Unit).HasMaxLength(10);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => new { x.ProjectId, x.Name });
            e.HasIndex(x => x.IsDeleted);
        });

        // Store
        modelBuilder.Entity<Store>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.StoreType).HasMaxLength(20).IsRequired();
            e.Property(x => x.AddressLine1).HasMaxLength(500);
            e.Property(x => x.AddressLine2).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(200);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Country).HasMaxLength(2);
            e.Property(x => x.ContactEmail).HasMaxLength(254);
            e.Property(x => x.ContactPhone).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.HasIndex(x => new { x.ProjectId, x.Code }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.IsDeleted);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });

        // Area
        modelBuilder.Entity<Area>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Depth).HasDefaultValue(0);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.ParentAreaId);
            e.HasIndex(x => x.IsDeleted);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.ParentArea).WithMany(x => x.Children).HasForeignKey(x => x.ParentAreaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AreaStore (composite key)
        modelBuilder.Entity<AreaStore>(e =>
        {
            e.HasKey(x => new { x.AreaId, x.StoreId });
            e.HasOne(x => x.Area).WithMany(x => x.AreaStores).HasForeignKey(x => x.AreaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
