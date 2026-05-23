namespace LootNet_API.Services.Interfaces;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(string email, string username, string verificationUrl);
    Task SendPasswordResetAsync(string email, string username, string resetUrl);
}
