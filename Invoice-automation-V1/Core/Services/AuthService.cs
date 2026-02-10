using InvoiceAutomation.Core.DTOs;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace InvoiceAutomation.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _tokenRepository;

    public AuthService(IUserRepository userRepository, IUserTokenRepository tokenRepository)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return new AuthResult
            {
                Success = false,
                Error = "Email already registered"
            };
        }

        // Check if this is the first user
        var isFirstUser = await IsFirstUserAsync();

        // Create user
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Phone = dto.Phone,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsSuperAdmin = isFirstUser,
            EmailConfirmed = false
        };

        await _userRepository.AddAsync(user);

        return new AuthResult
        {
            Success = true,
            UserId = user.Id
        };
    }

    public async Task<AuthResult> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Please verify your email address before logging in"
            };
        }

        // Check if account is locked
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Account is locked. Please try again later."
            };
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            }
            await _userRepository.UpdateAsync(user);

            return new AuthResult
            {
                Success = false,
                Error = "Invalid email or password"
            };
        }

        // Reset failed login attempts on successful login
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await UpdateLastLoginAsync(user.Id);

        return new AuthResult
        {
            Success = true,
            UserId = user.Id
        };
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token)
    {
        var tokenHash = HashToken(token);
        var userToken = await _tokenRepository.GetByTokenHashAsync(tokenHash);

        if (userToken == null ||
            userToken.TokenType != TokenType.EmailVerification ||
            userToken.UserId.ToString() != userId ||
            userToken.ExpiresAt < DateTime.UtcNow ||
            userToken.UsedAt != null)
        {
            return false;
        }

        // Mark token as used
        userToken.UsedAt = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(userToken);

        // Verify user email
        var user = await _userRepository.GetByIdAsync(userToken.UserId);
        if (user != null)
        {
            user.EmailConfirmed = true;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        return false;
    }

    public async Task<bool> SendPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal if email exists
            return true;
        }

        // Generate reset token
        var token = GenerateSecureToken();
        var userToken = new UserToken
        {
            UserId = user.Id,
            TokenType = TokenType.PasswordReset,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await _tokenRepository.AddAsync(userToken);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var tokenHash = HashToken(dto.Token);
        var userToken = await _tokenRepository.GetByTokenHashAsync(tokenHash);

        if (userToken == null ||
            userToken.TokenType != TokenType.PasswordReset ||
            userToken.UserId.ToString() != dto.UserId ||
            userToken.ExpiresAt < DateTime.UtcNow ||
            userToken.UsedAt != null)
        {
            return false;
        }

        // Mark token as used
        userToken.UsedAt = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(userToken);

        // Update password
        var user = await _userRepository.GetByIdAsync(userToken.UserId);
        if (user != null)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        return false;
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId)
    {
        var token = GenerateSecureToken();
        var userToken = new UserToken
        {
            UserId = userId,
            TokenType = TokenType.EmailVerification,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _tokenRepository.AddAsync(userToken);
        return token;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId)
    {
        var token = GenerateSecureToken();
        var userToken = new UserToken
        {
            UserId = userId,
            TokenType = TokenType.PasswordReset,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await _tokenRepository.AddAsync(userToken);
        return token;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<bool> IsFirstUserAsync()
    {
        var count = await _userRepository.GetCountAsync();
        return count == 0;
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
