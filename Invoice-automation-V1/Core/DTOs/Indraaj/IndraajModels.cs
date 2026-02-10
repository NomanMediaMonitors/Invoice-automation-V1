namespace InvoiceAutomation.Core.DTOs.Indraaj;

public class IndraajApiResponse<T>
{
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSuccessFull { get; set; }
    public T? Data { get; set; }
}

public class IndraajCoaItem
{
    public int Recno { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccountType { get; set; }
    public string? ParentCode { get; set; }
    public bool IsActive { get; set; } = true;

    public string DisplayName => $"{Code} - {Name}";
}

public class CoaSyncResult
{
    public bool Success { get; set; }
    public int TotalAccounts { get; set; }
    public int NewAccounts { get; set; }
    public int UpdatedAccounts { get; set; }
    public DateTime SyncedAt { get; set; }
    public string? Error { get; set; }
}
