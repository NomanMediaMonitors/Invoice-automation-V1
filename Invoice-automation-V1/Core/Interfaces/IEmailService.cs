namespace InvoiceAutomation.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl);
    Task SendPasswordResetAsync(string toEmail, string fullName, string resetUrl);
    Task SendWelcomeEmailAsync(string toEmail, string fullName);
}
