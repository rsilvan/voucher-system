namespace VoucherSystem.Contracts;

public class ErrorResponse
{
    public string Error { get; set; } = default!;
    public string? Detail { get; set; }
}
