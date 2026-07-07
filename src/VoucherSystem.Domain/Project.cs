namespace VoucherSystem.Domain;

public class Project
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = nameof(ProjectEnvironment.Production);
    public string Currency { get; set; } = "BRL";
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public string Locale { get; set; } = "pt-BR";
    public string Country { get; set; } = "BR";
    public string Status { get; set; } = nameof(ProjectStatus.Active);
    public bool IsPrimary { get; set; }
    public bool IsInUse { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }

    public bool CanChangeEnvironment()
        => !IsInUse;

    public bool CanChangeCurrency()
        => !IsInUse;

    public string GetStatusDisplay()
        => Status switch
        {
            "Active" => "Ativo",
            "Disabled" => "Desativado",
            "Archived" => "Arquivado",
            "PendingDeletion" => "Exclusão Pendente",
            _ => Status,
        };

    public string GetEnvironmentDisplay()
        => Environment switch
        {
            "Sandbox" => "Sandbox",
            "Development" => "Desenvolvimento",
            "Staging" => "Homologação",
            "Production" => "Produção",
            _ => Environment,
        };
}
