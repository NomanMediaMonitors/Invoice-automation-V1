using InvoiceAutomation.Core.DTOs.Employee;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceAutomation.Core.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserCompanyRepository _userCompanyRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IUserRepository userRepository,
        IUserCompanyRepository userCompanyRepository,
        ICompanyRepository companyRepository,
        IAuthService authService,
        IEmailService emailService,
        ILogger<EmployeeService> logger)
    {
        _userRepository = userRepository;
        _userCompanyRepository = userCompanyRepository;
        _companyRepository = companyRepository;
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<EmployeeListDto>> GetCompanyEmployeesAsync(Guid companyId)
    {
        var userCompanies = await _userCompanyRepository.GetByCompanyIdAsync(companyId);

        return userCompanies.Select(uc => new EmployeeListDto
        {
            UserId = uc.UserId,
            UserCompanyId = uc.Id,
            FullName = uc.User.FullName,
            Email = uc.User.Email,
            Phone = uc.User.Phone,
            Role = uc.Role,
            IsActive = uc.IsActive,
            IsUserDefault = uc.IsUserDefault,
            JoinedAt = uc.JoinedAt,
            LastLoginAt = uc.User.LastLoginAt,
            AvatarUrl = uc.User.AvatarUrl
        }).ToList();
    }

    public async Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            return null;
        }

        var invitedByUser = userCompany.InvitedBy.HasValue
            ? await _userRepository.GetByIdAsync(userCompany.InvitedBy.Value)
            : null;

        return new EmployeeDetailsDto
        {
            UserId = userCompany.UserId,
            UserCompanyId = userCompany.Id,
            FullName = userCompany.User.FullName,
            Email = userCompany.User.Email,
            Phone = userCompany.User.Phone,
            AvatarUrl = userCompany.User.AvatarUrl,
            Role = userCompany.Role,
            IsActive = userCompany.IsActive,
            IsUserDefault = userCompany.IsUserDefault,
            JoinedAt = userCompany.JoinedAt,
            InvitedAt = userCompany.InvitedAt,
            InvitedBy = userCompany.InvitedBy,
            InvitedByName = invitedByUser?.FullName,
            LastLoginAt = userCompany.User.LastLoginAt,
            CreatedAt = userCompany.CreatedAt
        };
    }

    public async Task<bool> InviteEmployeeAsync(InviteEmployeeDto dto, Guid invitedByUserId)
    {
        // Check if company exists
        var company = await _companyRepository.GetByIdAsync(dto.CompanyId);
        if (company == null)
        {
            throw new InvalidOperationException("Company not found");
        }

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);

        if (existingUser != null)
        {
            // User exists, check if already in this company
            var existingUserCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(
                existingUser.Id, dto.CompanyId);

            if (existingUserCompany != null)
            {
                throw new InvalidOperationException("This user is already part of the company");
            }

            // Add existing user to company
            var userCompany = new UserCompany
            {
                UserId = existingUser.Id,
                CompanyId = dto.CompanyId,
                Role = dto.Role,
                IsActive = true,
                InvitedBy = invitedByUserId,
                InvitedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow
            };

            await _userCompanyRepository.AddAsync(userCompany);

            // Send notification email
            await _emailService.SendEmailAsync(
                dto.Email,
                "You've been added to a company",
                $"You have been added to {company.Name} as {dto.Role}. You can now access this company from your dashboard."
            );

            _logger.LogInformation("Existing user {UserId} added to company {CompanyId} as {Role}",
                existingUser.Id, dto.CompanyId, dto.Role);

            return true;
        }
        else
        {
            // User doesn't exist, create new user and send invitation
            var tempPassword = GenerateTemporaryPassword();

            var registerDto = new Core.DTOs.RegisterDto
            {
                Email = dto.Email,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Password = tempPassword
            };

            var result = await _authService.RegisterAsync(registerDto);
            if (!result.Success || !result.UserId.HasValue)
            {
                throw new InvalidOperationException(result.Error ?? "Failed to create user account");
            }

            // Manually verify the email for invited users
            var user = await _userRepository.GetByIdAsync(result.UserId.Value);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userRepository.UpdateAsync(user);
            }

            // Add user to company
            var userCompany = new UserCompany
            {
                UserId = result.UserId.Value,
                CompanyId = dto.CompanyId,
                Role = dto.Role,
                IsActive = true,
                InvitedBy = invitedByUserId,
                InvitedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow
            };

            await _userCompanyRepository.AddAsync(userCompany);

            // Send invitation email with temporary password
            await _emailService.SendEmailAsync(
                dto.Email,
                "Welcome to Invoice Automation System",
                $@"You have been invited to join {company.Name} as {dto.Role}.

                Your account has been created with the following credentials:
                Email: {dto.Email}
                Temporary Password: {tempPassword}

                Please log in and change your password immediately.

                Login at: [Your App URL]/Account/Login"
            );

            _logger.LogInformation("New user {UserId} invited to company {CompanyId} as {Role}",
                result.UserId.Value, dto.CompanyId, dto.Role);

            return true;
        }
    }

    public async Task UpdateEmployeeRoleAsync(Guid companyId, Guid userId, UpdateEmployeeRoleDto dto)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("Employee not found in this company");
        }

        userCompany.Role = dto.Role;
        await _userCompanyRepository.UpdateAsync(userCompany);

        _logger.LogInformation("Employee {UserId} role updated to {Role} in company {CompanyId}",
            userId, dto.Role, companyId);
    }

    public async Task ActivateEmployeeAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("Employee not found in this company");
        }

        userCompany.IsActive = true;
        await _userCompanyRepository.UpdateAsync(userCompany);

        _logger.LogInformation("Employee {UserId} activated in company {CompanyId}", userId, companyId);
    }

    public async Task DeactivateEmployeeAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("Employee not found in this company");
        }

        userCompany.IsActive = false;
        await _userCompanyRepository.UpdateAsync(userCompany);

        _logger.LogInformation("Employee {UserId} deactivated in company {CompanyId}", userId, companyId);
    }

    public async Task RemoveEmployeeAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("Employee not found in this company");
        }

        await _userCompanyRepository.DeleteAsync(userCompany);

        _logger.LogInformation("Employee {UserId} removed from company {CompanyId}", userId, companyId);
    }

    public async Task<bool> IsUserInCompanyAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        return userCompany != null;
    }

    public async Task<bool> CanManageEmployeesAsync(Guid companyId, Guid currentUserId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(currentUserId, companyId);
        if (userCompany == null)
        {
            return false;
        }

        // Only Admin and SuperAdmin can manage employees
        return userCompany.Role == UserRole.Admin || userCompany.Role == UserRole.SuperAdmin;
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Range(0, 12)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
