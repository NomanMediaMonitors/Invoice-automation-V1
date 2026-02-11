using InvoiceAutomation.Core.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace InvoiceAutomation.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl)
    {
        var subject = "Verify Your Email - Invoice Automation System";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to Invoice Automation System!</h2>
                <p>Hello {fullName},</p>
                <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                <p style='margin: 20px 0;'>
                    <a href='{verificationUrl}' style='background-color: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Verify Email</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{verificationUrl}</p>
                <p>This link will expire in 7 days.</p>
                <p>If you didn't create an account, please ignore this email.</p>
                <br>
                <p>Best regards,<br>Invoice Automation System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string fullName, string resetUrl)
    {
        var subject = "Reset Your Password - Invoice Automation System";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Password Reset Request</h2>
                <p>Hello {fullName},</p>
                <p>We received a request to reset your password. Click the link below to create a new password:</p>
                <p style='margin: 20px 0;'>
                    <a href='{resetUrl}' style='background-color: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Reset Password</a>
                </p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                <br>
                <p>Best regards,<br>Invoice Automation System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
    {
        var subject = "Welcome to Invoice Automation System!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to Invoice Automation System!</h2>
                <p>Hello {fullName},</p>
                <p>Your email has been successfully verified. You can now log in and start using the system.</p>
                <p>Features you can explore:</p>
                <ul>
                    <li>Upload and process invoices with OCR</li>
                    <li>Manage vendors and companies</li>
                    <li>Track approvals and payments</li>
                    <li>Generate reports and analytics</li>
                </ul>
                <br>
                <p>Best regards,<br>Invoice Automation System Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["Email:FromName"] ?? "Invoice Automation System",
                _configuration["Email:FromAddress"] ?? "noreply@invoiceautomation.com"
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _configuration["Email:SmtpHost"] ?? "smtp.gmail.com",
                int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            // Authenticate
            await client.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]
            );

            // Send email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log the error (in production, use proper logging)
            Console.WriteLine($"Failed to send email: {ex.Message}");
            // In development, you might want to just log and continue
            // In production, you might want to throw or handle differently
        }
    }
}
