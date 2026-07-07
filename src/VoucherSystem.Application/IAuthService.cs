using VoucherSystem.Contracts;

namespace VoucherSystem.Application;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task LogoutAsync(string refreshToken);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
