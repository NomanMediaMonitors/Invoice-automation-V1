using InvoiceAutomation.Core.DTOs;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterDto dto);
    Task<AuthResult> LoginAsync(LoginDto dto);
    Task<bool> VerifyEmailAsync(string userId, string token);
    Task<bool> SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> IsFirstUserAsync();
    Task UpdateLastLoginAsync(Guid userId);
}
