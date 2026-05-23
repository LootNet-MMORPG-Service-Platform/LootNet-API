using System.Net.Http.Headers;
using System.Net.Http.Json;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ResendEmailSender(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendEmailVerificationAsync(string email, string username, string verificationUrl)
    {
        await SendAsync(
            email,
            "Verify your LootNet email",
            $"<p>Hi {username},</p><p>Verify your LootNet account by opening this link:</p><p><a href=\"{verificationUrl}\">{verificationUrl}</a></p><p>This link expires in 24 hours.</p>");
    }

    public async Task SendPasswordResetAsync(string email, string username, string resetUrl)
    {
        await SendAsync(
            email,
            "Reset your LootNet password",
            $"<p>Hi {username},</p><p>Reset your LootNet password by opening this link:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p><p>This link expires in 1 hour. If you did not request this, ignore this email.</p>");
    }

    private async Task SendAsync(string email, string subject, string html)
    {
        var apiKey = _config["Email:Resend:ApiKey"];
        var from = _config["Email:From"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Resend email is not configured. Set Email__Resend__ApiKey and Email__From.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from,
                to = email,
                subject,
                html
            })
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend email failed with status {(int)response.StatusCode}: {body}");
        }
    }
}
