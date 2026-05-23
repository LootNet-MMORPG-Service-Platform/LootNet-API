using System.Net;
using System.Net.Mail;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailVerificationAsync(string email, string username, string verificationUrl)
    {
        var host = _config["Email:Smtp:Host"];
        var from = _config["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP email is not configured. Set Email__Smtp__Host and Email__From.");

        var port = int.TryParse(_config["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 587;
        var usernameConfig = _config["Email:Smtp:Username"];
        var password = _config["Email:Smtp:Password"];
        var enableSsl = !bool.TryParse(_config["Email:Smtp:EnableSsl"], out var configuredSsl) || configuredSsl;

        using var message = new MailMessage(from, email)
        {
            Subject = "Verify your LootNet email",
            Body = $"Hi {username},\n\nVerify your LootNet account by opening this link:\n{verificationUrl}\n\nThis link expires in 24 hours.",
            IsBodyHtml = false
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(usernameConfig))
            client.Credentials = new NetworkCredential(usernameConfig, password);

        await client.SendMailAsync(message);
    }
}
