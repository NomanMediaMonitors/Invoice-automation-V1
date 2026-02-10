using InvoiceAutomation.Core.DTOs.Indraaj;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace InvoiceAutomation.Infrastructure.Services;

public class IndraajSyncService : IIndraajSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ICompanyRepository _companyRepository;
    private readonly IChartOfAccountRepository _coaRepository;
    private readonly ILogger<IndraajSyncService> _logger;
    private const string BaseUrl = "https://apiuae.indraaj.com/en/WebHooks";
    private const int MinDaysBetweenSync = 7;

    public IndraajSyncService(
        HttpClient httpClient,
        ICompanyRepository companyRepository,
        IChartOfAccountRepository coaRepository,
        ILogger<IndraajSyncService> logger)
    {
        _httpClient = httpClient;
        _companyRepository = companyRepository;
        _coaRepository = coaRepository;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("AccessTokenCode", accessToken);

            var response = await _httpClient.GetAsync($"{BaseUrl}/GetDetailedCoas");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Indraaj connection");
            return false;
        }
    }

    public async Task<CoaSyncResult> SyncChartOfAccountsAsync(Guid companyId)
    {
        var result = new CoaSyncResult
        {
            SyncedAt = DateTime.UtcNow
        };

        try
        {
            // Get company with access token
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null)
            {
                result.Success = false;
                result.Error = "Company not found";
                return result;
            }

            if (string.IsNullOrEmpty(company.IndraajAccessToken))
            {
                result.Success = false;
                result.Error = "Indraaj access token not configured";
                return result;
            }

            // Check if can sync (prevent too frequent syncs)
            if (!await CanSyncAsync(companyId))
            {
                result.Success = false;
                result.Error = $"Cannot sync. Last sync was less than {MinDaysBetweenSync} days ago";
                return result;
            }

            _logger.LogInformation("Starting COA sync for company {CompanyId}", companyId);

            // Fetch from Indraaj API
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("AccessTokenCode", company.IndraajAccessToken);

            var response = await _httpClient.GetAsync($"{BaseUrl}/GetDetailedCoas");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content
                .ReadFromJsonAsync<IndraajApiResponse<List<IndraajCoaItem>>>();

            if (apiResponse?.IsSuccessFull != true || apiResponse.Data == null)
            {
                result.Success = false;
                result.Error = apiResponse?.Message ?? "Failed to fetch data from Indraaj";
                return result;
            }

            result.TotalAccounts = apiResponse.Data.Count;

            // Get existing accounts for comparison
            var existingAccounts = await _coaRepository.GetByCompanyIdAsync(companyId);
            var existingRecnos = existingAccounts.Select(a => a.Recno).ToHashSet();

            // Process accounts
            foreach (var item in apiResponse.Data.Where(a => a.IsActive))
            {
                var existingAccount = existingAccounts.FirstOrDefault(a => a.Recno == item.Recno);

                if (existingAccount == null)
                {
                    // New account
                    var newAccount = new ChartOfAccount
                    {
                        CompanyId = companyId,
                        Recno = item.Recno,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        AccountType = item.AccountType,
                        ParentCode = item.ParentCode,
                        IsActive = item.IsActive,
                        SyncedAt = DateTime.UtcNow
                    };

                    await _coaRepository.AddAsync(newAccount);
                    result.NewAccounts++;
                }
                else
                {
                    // Update existing account if changed
                    bool changed = false;

                    if (existingAccount.Code != item.Code)
                    {
                        existingAccount.Code = item.Code;
                        changed = true;
                    }

                    if (existingAccount.Name != item.Name)
                    {
                        existingAccount.Name = item.Name;
                        changed = true;
                    }

                    if (existingAccount.Description != item.Description)
                    {
                        existingAccount.Description = item.Description;
                        changed = true;
                    }

                    if (existingAccount.AccountType != item.AccountType)
                    {
                        existingAccount.AccountType = item.AccountType;
                        changed = true;
                    }

                    if (existingAccount.ParentCode != item.ParentCode)
                    {
                        existingAccount.ParentCode = item.ParentCode;
                        changed = true;
                    }

                    if (existingAccount.IsActive != item.IsActive)
                    {
                        existingAccount.IsActive = item.IsActive;
                        changed = true;
                    }

                    if (changed)
                    {
                        existingAccount.SyncedAt = DateTime.UtcNow;
                        existingAccount.UpdatedAt = DateTime.UtcNow;
                        await _coaRepository.UpdateAsync(existingAccount);
                        result.UpdatedAccounts++;
                    }
                }
            }

            // Update company's last sync time
            company.LastCoaSyncAt = DateTime.UtcNow;
            await _companyRepository.UpdateAsync(company);

            result.Success = true;
            _logger.LogInformation(
                "COA sync completed for company {CompanyId}. Total: {Total}, New: {New}, Updated: {Updated}",
                companyId, result.TotalAccounts, result.NewAccounts, result.UpdatedAccounts);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync COA for company {CompanyId}", companyId);
            result.Success = false;
            result.Error = $"Sync failed: {ex.Message}";
            return result;
        }
    }

    public async Task<List<IndraajCoaItem>> GetLocalChartOfAccountsAsync(Guid companyId)
    {
        var accounts = await _coaRepository.GetByCompanyIdAsync(companyId);

        return accounts
            .Where(a => a.IsActive)
            .Select(a => new IndraajCoaItem
            {
                Recno = a.Recno,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                AccountType = a.AccountType,
                ParentCode = a.ParentCode,
                IsActive = a.IsActive
            })
            .OrderBy(a => a.Code)
            .ToList();
    }

    public async Task<List<IndraajCoaItem>> GetExpenseAccountsAsync(Guid companyId)
    {
        var accounts = await _coaRepository.GetExpenseAccountsAsync(companyId);

        return accounts
            .Where(a => a.IsActive)
            .Select(a => new IndraajCoaItem
            {
                Recno = a.Recno,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                AccountType = a.AccountType,
                ParentCode = a.ParentCode,
                IsActive = a.IsActive
            })
            .OrderBy(a => a.Code)
            .ToList();
    }

    public async Task<List<IndraajCoaItem>> GetBankCashAccountsAsync(Guid companyId)
    {
        var accounts = await _coaRepository.GetBankCashAccountsAsync(companyId);

        return accounts
            .Where(a => a.IsActive)
            .Select(a => new IndraajCoaItem
            {
                Recno = a.Recno,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                AccountType = a.AccountType,
                ParentCode = a.ParentCode,
                IsActive = a.IsActive
            })
            .OrderBy(a => a.Code)
            .ToList();
    }

    public async Task<bool> CanSyncAsync(Guid companyId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null) return false;

        // Allow sync if never synced before
        if (!company.LastCoaSyncAt.HasValue) return true;

        // Check if last sync was more than MinDaysBetweenSync days ago
        var daysSinceLastSync = (DateTime.UtcNow - company.LastCoaSyncAt.Value).TotalDays;
        return daysSinceLastSync >= MinDaysBetweenSync;
    }

    public async Task<int?> GetDaysSinceLastSyncAsync(Guid companyId)
    {
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null || !company.LastCoaSyncAt.HasValue)
            return null;

        return (int)(DateTime.UtcNow - company.LastCoaSyncAt.Value).TotalDays;
    }
}
