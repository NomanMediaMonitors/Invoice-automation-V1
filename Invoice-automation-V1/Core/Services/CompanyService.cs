using InvoiceAutomation.Core.DTOs.Company;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceAutomation.Core.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserCompanyRepository _userCompanyRepository;
    private readonly IIndraajSyncService _indraajSyncService;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        ICompanyRepository companyRepository,
        IUserCompanyRepository userCompanyRepository,
        IIndraajSyncService indraajSyncService,
        ILogger<CompanyService> logger)
    {
        _companyRepository = companyRepository;
        _userCompanyRepository = userCompanyRepository;
        _indraajSyncService = indraajSyncService;
        _logger = logger;
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _companyRepository.GetByIdAsync(id);
    }

    public async Task<Company?> GetByNtnAsync(string ntn)
    {
        return await _companyRepository.GetByNtnAsync(ntn);
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _companyRepository.GetAllAsync();
    }

    public async Task<Company> CreateAsync(CreateCompanyDto dto, Guid createdByUserId)
    {
        // Check if NTN already exists
        if (await _companyRepository.NtnExistsAsync(dto.Ntn))
        {
            throw new InvalidOperationException("A company with this NTN already exists");
        }

        // Check if this is the first company for the system
        var companyCount = await _companyRepository.GetCountAsync();
        var isFirstCompany = companyCount == 0;

        // Create company
        var company = new Company
        {
            Name = dto.Name,
            Ntn = dto.Ntn,
            Strn = dto.Strn,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            IsDefault = isFirstCompany,
            DisplayOrder = companyCount + 1
        };

        company = await _companyRepository.AddAsync(company);

        // Add creator as admin
        var userCompany = new UserCompany
        {
            UserId = createdByUserId,
            CompanyId = company.Id,
            Role = UserRole.Admin,
            IsUserDefault = isFirstCompany // First company is user's default
        };

        await _userCompanyRepository.AddAsync(userCompany);

        _logger.LogInformation("Company {CompanyId} created by user {UserId}", company.Id, createdByUserId);

        return company;
    }

    public async Task<Company> UpdateAsync(Guid id, UpdateCompanyDto dto)
    {
        var company = await _companyRepository.GetByIdAsync(id);
        if (company == null)
        {
            throw new InvalidOperationException("Company not found");
        }

        // Check if NTN is being changed and if new NTN already exists
        if (company.Ntn != dto.Ntn && await _companyRepository.NtnExistsAsync(dto.Ntn))
        {
            throw new InvalidOperationException("A company with this NTN already exists");
        }

        company.Name = dto.Name;
        company.Ntn = dto.Ntn;
        company.Strn = dto.Strn;
        company.Address = dto.Address;
        company.Phone = dto.Phone;
        company.Email = dto.Email;

        await _companyRepository.UpdateAsync(company);

        _logger.LogInformation("Company {CompanyId} updated", company.Id);

        return company;
    }

    public async Task<bool> NtnExistsAsync(string ntn)
    {
        return await _companyRepository.NtnExistsAsync(ntn);
    }

    public async Task<List<CompanyDropdownDto>> GetUserCompaniesAsync(Guid userId)
    {
        var userCompanies = await _userCompanyRepository.GetByUserIdAsync(userId);

        var dtos = new List<CompanyDropdownDto>();

        foreach (var uc in userCompanies)
        {
            var daysSinceSync = await _indraajSyncService.GetDaysSinceLastSyncAsync(uc.CompanyId);

            dtos.Add(new CompanyDropdownDto
            {
                Id = uc.Company.Id,
                Name = uc.Company.Name,
                Ntn = uc.Company.Ntn,
                IsUserDefault = uc.IsUserDefault,
                Role = uc.Role,
                LastCoaSyncAt = uc.Company.LastCoaSyncAt,
                HasIndraajConnection = !string.IsNullOrEmpty(uc.Company.IndraajAccessToken)
            });
        }

        return dtos;
    }

    public async Task<Company?> GetUserDefaultCompanyAsync(Guid userId)
    {
        var userCompany = await _userCompanyRepository.GetUserDefaultAsync(userId);
        return userCompany?.Company;
    }

    public async Task SetUserDefaultCompanyAsync(Guid userId, Guid companyId)
    {
        // Verify user has access to this company
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("User does not have access to this company");
        }

        // Clear existing defaults
        await _userCompanyRepository.ClearUserDefaultAsync(userId);

        // Set new default
        userCompany.IsUserDefault = true;
        await _userCompanyRepository.UpdateAsync(userCompany);

        _logger.LogInformation("User {UserId} set company {CompanyId} as default", userId, companyId);
    }

    public async Task<UserCompany?> GetUserCompanyAsync(Guid userId, Guid companyId)
    {
        return await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
    }

    public async Task AddUserToCompanyAsync(Guid userId, Guid companyId, UserRole role)
    {
        // Check if relationship already exists
        var existing = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (existing != null)
        {
            throw new InvalidOperationException("User is already assigned to this company");
        }

        // Check if user has any companies - if not, this will be their default
        var userCompanies = await _userCompanyRepository.GetByUserIdAsync(userId);
        var isFirstCompany = userCompanies.Count == 0;

        var userCompany = new UserCompany
        {
            UserId = userId,
            CompanyId = companyId,
            Role = role,
            IsUserDefault = isFirstCompany
        };

        await _userCompanyRepository.AddAsync(userCompany);

        _logger.LogInformation("User {UserId} added to company {CompanyId} with role {Role}",
            userId, companyId, role);
    }

    public async Task UpdateUserRoleAsync(Guid userId, Guid companyId, UserRole role)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("User is not assigned to this company");
        }

        userCompany.Role = role;
        await _userCompanyRepository.UpdateAsync(userCompany);

        _logger.LogInformation("User {UserId} role updated to {Role} for company {CompanyId}",
            userId, role, companyId);
    }

    public async Task RemoveUserFromCompanyAsync(Guid userId, Guid companyId)
    {
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(userId, companyId);
        if (userCompany == null)
        {
            throw new InvalidOperationException("User is not assigned to this company");
        }

        var wasDefault = userCompany.IsUserDefault;
        await _userCompanyRepository.DeleteAsync(userCompany);

        // If this was the default company, set another one as default
        if (wasDefault)
        {
            var remainingCompanies = await _userCompanyRepository.GetByUserIdAsync(userId);
            if (remainingCompanies.Count > 0)
            {
                var newDefault = remainingCompanies.First();
                newDefault.IsUserDefault = true;
                await _userCompanyRepository.UpdateAsync(newDefault);
            }
        }

        _logger.LogInformation("User {UserId} removed from company {CompanyId}", userId, companyId);
    }

    public async Task<List<UserCompany>> GetCompanyUsersAsync(Guid companyId)
    {
        return await _userCompanyRepository.GetByCompanyIdAsync(companyId);
    }

    public async Task<bool> ConnectIndraajAsync(Guid companyId, ConnectIndraajDto dto)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null)
        {
            throw new InvalidOperationException("Company not found");
        }

        // Test the connection first
        var isValid = await _indraajSyncService.TestConnectionAsync(dto.AccessToken);
        if (!isValid)
        {
            _logger.LogWarning("Failed to validate Indraaj access token for company {CompanyId}", companyId);
            return false;
        }

        // Save the access token
        company.IndraajAccessToken = dto.AccessToken;
        await _companyRepository.UpdateAsync(company);

        _logger.LogInformation("Indraaj access token configured for company {CompanyId}", companyId);

        return true;
    }

    public async Task<bool> TestIndraajConnectionAsync(Guid companyId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null || string.IsNullOrEmpty(company.IndraajAccessToken))
        {
            return false;
        }

        return await _indraajSyncService.TestConnectionAsync(company.IndraajAccessToken);
    }
}
