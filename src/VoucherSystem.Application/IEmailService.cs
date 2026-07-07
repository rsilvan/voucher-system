namespace VoucherSystem.Application;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "sandbox.smtp.mailtrap.io";
    public int SmtpPort { get; set; } = 2525;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromName { get; set; } = "Voucher System";
    public string FromEmail { get; set; } = "noreply@vouchersystem.com";
}

public interface IEmailService
{
    Task SendInvitationEmailAsync(string toEmail, string toName, string invitationToken, string organizationName);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
    Task SendWelcomeEmailAsync(string toEmail, string toName, string organizationName);
}
