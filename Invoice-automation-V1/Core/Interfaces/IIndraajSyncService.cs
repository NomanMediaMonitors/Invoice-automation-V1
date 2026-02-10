using InvoiceAutomation.Core.DTOs.Indraaj;

namespace InvoiceAutomation.Core.Interfaces;

public interface IIndraajSyncService
{
    /// <summary>
    /// Test connection to Indraaj API with provided access token
    /// </summary>
    Task<bool> TestConnectionAsync(string accessToken);

    /// <summary>
    /// Sync Chart of Accounts from Indraaj API to local database
    /// This should only be called if last sync was more than 7 days ago
    /// </summary>
    Task<CoaSyncResult> SyncChartOfAccountsAsync(Guid companyId);

    /// <summary>
    /// Get Chart of Accounts from local database
    /// </summary>
    Task<List<IndraajCoaItem>> GetLocalChartOfAccountsAsync(Guid companyId);

    /// <summary>
    /// Get expense accounts from local database
    /// </summary>
    Task<List<IndraajCoaItem>> GetExpenseAccountsAsync(Guid companyId);

    /// <summary>
    /// Get bank/cash accounts from local database
    /// </summary>
    Task<List<IndraajCoaItem>> GetBankCashAccountsAsync(Guid companyId);

    /// <summary>
    /// Check if company can sync (last sync was more than 7 days ago)
    /// </summary>
    Task<bool> CanSyncAsync(Guid companyId);

    /// <summary>
    /// Get days since last sync
    /// </summary>
    Task<int?> GetDaysSinceLastSyncAsync(Guid companyId);
}
