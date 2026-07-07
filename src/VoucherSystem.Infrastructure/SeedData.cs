using Microsoft.EntityFrameworkCore;
using VoucherSystem.Domain;

namespace VoucherSystem.Infrastructure;

public static class SeedData
{
    public static async Task SeedAsync(VoucherSystemDbContext db)
    {
        if (await db.Plans.AnyAsync()) return; // already seeded

        var now = DateTimeOffset.UtcNow;

        // ─── Plans ───
        var trialPlan = new Plan
        {
            Id = Guid.NewGuid(),
            Key = "Trial",
            Name = "Trial",
            MonthlyPrice = 0,
            MaxUsers = 3,
            MaxProjects = 1,
            MaxActiveCampaigns = 5,
            MonthlyApiCalls = 10_000,
            AllowsCustomRoles = false,
            AllowsAuditExport = false,
            AllowsSso = false,
            CreatedAt = now
        };
        var freePlan = new Plan
        {
            Id = Guid.NewGuid(),
            Key = "Free",
            Name = "Free",
            MonthlyPrice = 0,
            MaxUsers = 3,
            MaxProjects = 1,
            MaxActiveCampaigns = 2,
            MonthlyApiCalls = 1_000,
            AllowsCustomRoles = false,
            AllowsAuditExport = false,
            AllowsSso = false,
            CreatedAt = now
        };

        db.Plans.AddRange(trialPlan, freePlan);

        // ─── Permissions ───
        var permissions = new List<Permission>
        {
            new() { Id = Guid.NewGuid(), Key = "organization.read", Resource = "organization", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "organization.update", Resource = "organization", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "organization.delete_request", Resource = "organization", Action = "delete_request", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "users.invite", Resource = "users", Action = "invite", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "users.read", Resource = "users", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "users.update", Resource = "users", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "users.disable", Resource = "users", Action = "disable", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "users.enable", Resource = "users", Action = "enable", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "roles.read", Resource = "roles", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "roles.create", Resource = "roles", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "roles.update", Resource = "roles", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "roles.delete", Resource = "roles", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "permissions.read", Resource = "permissions", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.create", Resource = "projects", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.read", Resource = "projects", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.update", Resource = "projects", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.delete", Resource = "projects", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.manage_production", Resource = "projects", Action = "manage_production", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "projects.promote", Resource = "projects", Action = "promote", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "api_keys.read", Resource = "api_keys", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "api_keys.create", Resource = "api_keys", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "api_keys.regenerate", Resource = "api_keys", Action = "regenerate", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "api_keys.revoke", Resource = "api_keys", Action = "revoke", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "webhooks.read", Resource = "webhooks", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "webhooks.create", Resource = "webhooks", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "webhooks.update", Resource = "webhooks", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "webhooks.delete", Resource = "webhooks", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "billing.read", Resource = "billing", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "billing.manage", Resource = "billing", Action = "manage", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "usage.read", Resource = "usage", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "audit.read", Resource = "audit", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "audit.export", Resource = "audit", Action = "export", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "security.manage", Resource = "security", Action = "manage", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "campaigns.read", Resource = "campaigns", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "campaigns.create", Resource = "campaigns", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "campaigns.update", Resource = "campaigns", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "campaigns.delete", Resource = "campaigns", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "vouchers.read", Resource = "vouchers", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "customers.read", Resource = "customers", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "redemptions.read", Resource = "redemptions", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "reports.read", Resource = "reports", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "events.read", Resource = "events", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "logs.read", Resource = "logs", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "metadata.read", Resource = "metadata", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "invoices.read", Resource = "invoices", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "vouchers.create", Resource = "vouchers", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "vouchers.update", Resource = "vouchers", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "vouchers.import", Resource = "vouchers", Action = "import", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "vouchers.export", Resource = "vouchers", Action = "export", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "redemptions.cancel", Resource = "redemptions", Action = "cancel", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "audit.read_project", Resource = "audit", Action = "read_project", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "webhooks.test", Resource = "webhooks", Action = "test", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "segments.read", Resource = "segments", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "segments.create", Resource = "segments", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "segments.update", Resource = "segments", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "customers.create", Resource = "customers", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "customers.update", Resource = "customers", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "validation_rules.read", Resource = "validation_rules", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "validation_rules.create", Resource = "validation_rules", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "validation_rules.update", Resource = "validation_rules", Action = "update", CreatedAt = now },
            // R002 — Brands, Stores, Areas, Locations
            new() { Id = Guid.NewGuid(), Key = "brands.read", Resource = "brands", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "brands.create", Resource = "brands", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "brands.update", Resource = "brands", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "brands.delete", Resource = "brands", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "stores.read", Resource = "stores", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "stores.create", Resource = "stores", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "stores.update", Resource = "stores", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "stores.delete", Resource = "stores", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "areas.read", Resource = "areas", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "areas.create", Resource = "areas", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "areas.update", Resource = "areas", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "areas.delete", Resource = "areas", Action = "delete", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "locations.read", Resource = "locations", Action = "read", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "locations.create", Resource = "locations", Action = "create", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "locations.update", Resource = "locations", Action = "update", CreatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "locations.delete", Resource = "locations", Action = "delete", CreatedAt = now },
        };

        db.Permissions.AddRange(permissions);
        var permIndex = permissions.ToDictionary(p => p.Key);

        // ─── System Roles ───

        // OrganizationOwner — all permissions
        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = null,
            Name = "OrganizationOwner",
            Key = "OrganizationOwner",
            Description = "Maximum role with full access to the organization",
            IsSystemRole = true,
            Scope = "Organization",
            CreatedAt = now
        };
        db.Roles.Add(ownerRole);

        var ownerPermKeys = new[]
        {
            "organization.read", "organization.update", "organization.delete_request",
            "users.invite", "users.read", "users.update", "users.disable", "users.enable",
            "roles.read", "roles.create", "roles.update", "roles.delete",
            "permissions.read",
            "projects.create", "projects.read", "projects.update", "projects.delete",
            "projects.manage_production", "projects.promote",
            "api_keys.read", "api_keys.create", "api_keys.regenerate", "api_keys.revoke",
            "webhooks.read", "webhooks.create", "webhooks.update", "webhooks.delete",
            "billing.read", "billing.manage",
            "usage.read", "audit.read", "audit.export",
            "security.manage",
            "campaigns.read", "campaigns.create", "campaigns.update", "campaigns.delete",
            "vouchers.read", "vouchers.create", "vouchers.update", "vouchers.import", "vouchers.export",
            "customers.read", "customers.create", "customers.update",
            "segments.read", "segments.create", "segments.update",
            "redemptions.read", "redemptions.cancel",
            "reports.read",
            "validation_rules.read", "validation_rules.create", "validation_rules.update",
            "events.read", "logs.read", "metadata.read", "invoices.read",
            "audit.read_project", "webhooks.test",
            "brands.read", "brands.create", "brands.update", "brands.delete",
            "stores.read", "stores.create", "stores.update", "stores.delete",
            "areas.read", "areas.create", "areas.update", "areas.delete",
            "locations.read", "locations.create", "locations.update", "locations.delete",
        };
        foreach (var k in ownerPermKeys)
        {
            if (permIndex.TryGetValue(k, out var perm))
                db.RolePermissions.Add(new RolePermission { RoleId = ownerRole.Id, PermissionId = perm.Id });
        }

        // OrganizationAdmin
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = null,
            Name = "OrganizationAdmin",
            Key = "OrganizationAdmin",
            Description = "Operational administrator of the organization",
            IsSystemRole = true,
            Scope = "Organization",
            CreatedAt = now
        };
        db.Roles.Add(adminRole);

        var adminPermKeys = new[]
        {
            "organization.read", "organization.update",
            "users.invite", "users.read", "users.update", "users.disable", "users.enable",
            "roles.read", "permissions.read",
            "projects.create", "projects.read", "projects.update",
            "projects.manage_production", "projects.promote",
            "api_keys.read", "api_keys.create", "api_keys.regenerate",
            "webhooks.read", "webhooks.create", "webhooks.update", "webhooks.delete",
            "usage.read", "audit.read",
            "campaigns.read", "campaigns.create", "campaigns.update", "campaigns.delete",
            "vouchers.read", "vouchers.create", "vouchers.update", "vouchers.import", "vouchers.export",
            "customers.read", "customers.create", "customers.update",
            "segments.read", "segments.create", "segments.update",
            "redemptions.read", "redemptions.cancel",
            "reports.read",
            "validation_rules.read", "validation_rules.create", "validation_rules.update",
            "events.read", "logs.read", "audit.read_project", "webhooks.test",
            "brands.read", "brands.create", "brands.update", "brands.delete",
            "stores.read", "stores.create", "stores.update", "stores.delete",
            "areas.read", "areas.create", "areas.update", "areas.delete",
            "locations.read", "locations.create", "locations.update", "locations.delete",
        };
        foreach (var k in adminPermKeys)
        {
            if (permIndex.TryGetValue(k, out var perm))
                db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
        }

        // ReadOnly
        var readonlyRole = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = null,
            Name = "ReadOnly",
            Key = "ReadOnly",
            Description = "Read-only access",
            IsSystemRole = true,
            Scope = "Organization",
            CreatedAt = now
        };
        db.Roles.Add(readonlyRole);

        var readonlyPermKeys = new[]
        {
            "organization.read", "projects.read", "campaigns.read",
            "vouchers.read", "customers.read", "segments.read",
            "redemptions.read", "reports.read",
            "brands.read", "stores.read", "areas.read", "locations.read",
        };
        foreach (var k in readonlyPermKeys)
        {
            if (permIndex.TryGetValue(k, out var perm))
                db.RolePermissions.Add(new RolePermission { RoleId = readonlyRole.Id, PermissionId = perm.Id });
        }

        await db.SaveChangesAsync();
    }
}
