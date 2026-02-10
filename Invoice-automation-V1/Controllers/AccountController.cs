using InvoiceAutomation.Core.DTOs;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceAutomation.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;

    public AccountController(IAuthService authService, IEmailService emailService)
    {
        _authService = authService;
        _emailService = emailService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new RegisterDto
        {
            Email = model.Email,
            Password = model.Password,
            FullName = model.FullName,
            Phone = model.Phone
        };

        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Registration failed");
            return View(model);
        }

        // Generate email verification token
        var token = await _authService.GenerateEmailVerificationTokenAsync(result.UserId!.Value);
        var verifyUrl = Url.Action("VerifyEmail", "Account",
            new { userId = result.UserId, token }, Request.Scheme);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(model.Email, model.FullName, verifyUrl!);

        TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
        return RedirectToAction("EmailConfirmationSent");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult EmailConfirmationSent()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return View("EmailVerificationFailed");
        }

        var success = await _authService.VerifyEmailAsync(userId, token);

        if (success)
        {
            // Get user details for welcome email
            var user = await _authService.GetUserByIdAsync(Guid.Parse(userId));
            if (user != null)
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
            }

            TempData["SuccessMessage"] = "Email verified successfully! You can now log in.";
            return View("EmailVerified");
        }

        return View("EmailVerificationFailed");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new LoginDto
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        };

        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Login failed");
            return View(model);
        }

        // Get user details
        var user = await _authService.GetUserByIdAsync(result.UserId!.Value);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found");
            return View(model);
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Update last login
        await _authService.UpdateLastLoginAsync(user.Id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _authService.SendPasswordResetAsync(model.Email);

        // Get user for reset email
        var user = await _authService.GetUserByEmailAsync(model.Email);
        if (user != null)
        {
            // Generate token
            var tokens = await _authService.GeneratePasswordResetTokenAsync(user.Id);
            var resetUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, token = tokens }, Request.Scheme);

            await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetUrl!);
        }

        TempData["SuccessMessage"] = "If an account with that email exists, a password reset link has been sent.";
        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login");
        }

        var model = new ResetPasswordViewModel
        {
            UserId = userId,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new ResetPasswordDto
        {
            UserId = model.UserId,
            Token = model.Token,
            NewPassword = model.NewPassword
        };

        var success = await _authService.ResetPasswordAsync(dto);

        if (success)
        {
            TempData["SuccessMessage"] = "Password reset successful! You can now log in with your new password.";
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", "Invalid or expired reset link. Please request a new one.");
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

// Extension to IAuthService for password reset token generation
public static class AuthServiceExtensions
{
    public static async Task<string> GeneratePasswordResetTokenAsync(this IAuthService authService, Guid userId)
    {
        // This would need to be implemented in the actual AuthService
        // For now, returning a placeholder
        return Guid.NewGuid().ToString();
    }
}
