namespace InvoiceAutomation.Core.Configuration;

public class TesseractSettings
{
    public string DataPath { get; set; } = "wwwroot/tessdata";
    public string Language { get; set; } = "eng";
}
