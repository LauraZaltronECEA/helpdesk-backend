using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace api.services.Handlers;

// SMTP server settings loaded from appsettings.json.
public class SmtpSettings
{
    // SMTP server hostname or IP.
    public string Host { get; set; } = string.Empty;

    // SMTP server port (default 587 for STARTTLS).
    public int Port { get; set; } = 587;

    // Whether to use SSL immediately on connect.
    public bool UseSsl { get; set; }

    // SMTP authentication username (empty = no auth).
    public string Username { get; set; } = string.Empty;

    // SMTP authentication password.
    public string Password { get; set; } = string.Empty;

    // "From" email address displayed to recipients.
    public string FromAddress { get; set; } = string.Empty;

    // "From" display name shown in the recipient's inbox.
    public string FromName { get; set; } = string.Empty;
}

// Sends transactional emails (confirmation, password reset) via SMTP using MailKit.
public class EmailHandler
{
    private readonly SmtpSettings _settings;

    public EmailHandler(IConfiguration configuration)
    {
        _settings = configuration.GetSection("Smtp").Get<SmtpSettings>()
            ?? throw new InvalidOperationException("SMTP configuration section 'Smtp' is missing.");
    }

    // Sends an HTML email with a confirmation link after user registration.
    public async Task SendConfirmationEmailAsync(string toEmail, string toName, string confirmationLink)
    {
        var body = $"""
        <html>
        <body>
        <h2>Bienvenido, {toName}!</h2>
        <p>Gracias por registrarte. Por favor confirma tu correo electronico haciendo clic en el siguiente enlace:</p>
        <p><a href='{confirmationLink}'>Confirmar correo electronico</a></p>
        </body>
        </html>
        """;

        await SendEmailAsync(toEmail, "Confirma tu correo electronico", body);
    }

    // Sends an HTML email with a password reset link.
    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var body = $"""
        <html>
        <body>
        <h2>Restablece tu contrasena</h2>
        <p>Hola, {toName}. Haz clic en el siguiente enlace para restablecer tu contrasena:</p>
        <p><a href='{resetLink}'>Restablecer contrasena</a></p>
        <p>Si no solicitaste este cambio, ignora este mensaje.</p>
        </body>
        </html>
        """;

        await SendEmailAsync(toEmail, "Restablece tu contrasena", body);
    }

    // Core method that builds and sends a MIME email via the configured SMTP server.
    private async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
