namespace VoucherSystem.Domain;

public class AreaStore
{
    public Guid AreaId { get; set; }
    public Guid StoreId { get; set; }

    public Area Area { get; set; } = default!;
    public Store Store { get; set; } = default!;
}
