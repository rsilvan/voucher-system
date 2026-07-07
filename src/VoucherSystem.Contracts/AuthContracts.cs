namespace VoucherSystem.Contracts;

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public UserInfo User { get; set; } = default!;
    public OrgInfo Organization { get; set; } = default!;
    public List<string> Permissions { get; set; } = new();
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
}

public class OrgInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = default!;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}
