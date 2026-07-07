using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoucherSystem.Application;

namespace VoucherSystem.Infrastructure;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(string toEmail, string toName, string invitationToken, string organizationName)
    {
        var acceptUrl = $"http://localhost:5173/invitations/{invitationToken}/accept";
        var subject = $"Convite para {organizationName}";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; background: #f8fafc; padding: 24px;'>
  <div style='max-width: 480px; margin: 0 auto; background: white; border-radius: 12px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>
    <h2 style='color: #1e293b; margin-top: 0;'>Convite para {organizationName}</h2>
    <p style='color: #475569;'>Olá {toName},</p>
    <p style='color: #475569;'>Você foi convidado para participar da organização <strong>{organizationName}</strong> no Voucher System.</p>
    <a href='{acceptUrl}' style='display: inline-block; background: #6366f1; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; margin: 16px 0;'>Aceitar Convite</a>
    <p style='color: #94a3b8; font-size: 12px;'>Este convite expira em 7 dias.</p>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var resetUrl = $"http://localhost:5173/reset-password?token={resetToken}";
        var subject = "Redefinição de Senha";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; background: #f8fafc; padding: 24px;'>
  <div style='max-width: 480px; margin: 0 auto; background: white; border-radius: 12px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>
    <h2 style='color: #1e293b; margin-top: 0;'>Redefinição de Senha</h2>
    <p style='color: #475569;'>Clique no link abaixo para redefinir sua senha:</p>
    <a href='{resetUrl}' style='display: inline-block; background: #6366f1; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; margin: 16px 0;'>Redefinir Senha</a>
    <p style='color: #94a3b8; font-size: 12px;'>Este link expira em 1 hora. Se você não solicitou esta alteração, ignore este e-mail.</p>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName, string organizationName)
    {
        var subject = $"Bem-vindo ao Voucher System, {toName}!";
        var body = $@"
<html>
<body style='font-family: Arial, sans-serif; background: #f8fafc; padding: 24px;'>
  <div style='max-width: 480px; margin: 0 auto; background: white; border-radius: 12px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>
    <h2 style='color: #1e293b; margin-top: 0;'>Bem-vindo, {toName}!</h2>
    <p style='color: #475569;'>Sua organização <strong>{organizationName}</strong> foi criada com sucesso no Voucher System.</p>
    <p style='color: #475569;'>Acesse o portal para começar a configurar seus projetos, criar campanhas e gerenciar incentivos.</p>
    <a href='http://localhost:5173/login' style='display: inline-block; background: #6366f1; color: white; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; margin: 16px 0;'>Acessar Portal</a>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = true,
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
