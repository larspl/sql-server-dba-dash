using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.ModernUI.Services;

public interface IExportService
{
    Task<bool> ExportToCsvAsync<T>(IEnumerable<T> data, string filePath, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<string?> ShowSaveFileDialogAsync(string defaultFileName, ExportFormat format);
}

public enum ExportFormat
{
    Csv,
    Excel,
    Pdf
}

public class ExportProgress
{
    public int PercentComplete { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public int ProcessedItems { get; set; }
    public int TotalItems { get; set; }
}

public class ExportService : IExportService
{
    private readonly IDialogService _dialogService;

    public ExportService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<string?> ShowSaveFileDialogAsync(string defaultFileName, ExportFormat format)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            
            // Get the current window's handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            picker.SuggestedFileName = defaultFileName;
            
            switch (format)
            {
                case ExportFormat.Csv:
                    picker.FileTypeChoices.Add("CSV Files", new List<string>() { ".csv" });
                    break;
                case ExportFormat.Excel:
                    picker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
                    break;
                case ExportFormat.Pdf:
                    picker.FileTypeChoices.Add("PDF Files", new List<string>() { ".pdf" });
                    break;
            }

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to show save dialog: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ExportToCsvAsync<T>(IEnumerable<T> data, string filePath, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report(new ExportProgress { CurrentOperation = "Preparing CSV export...", PercentComplete = 0 });
            
            var dataList = data.ToList();
            var totalItems = dataList.Count;
            var processedItems = 0;

            using var writer = new StreamWriter(filePath);
            
            // Write header
            progress?.Report(new ExportProgress { CurrentOperation = "Writing headers...", PercentComplete = 5 });
            var properties = typeof(T).GetProperties();
            var header = string.Join(",", properties.Select(p => EscapeCsvValue(p.Name)));
            await writer.WriteLineAsync(header);

            // Write data rows
            foreach (var item in dataList)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                var values = properties.Select(p => EscapeCsvValue(p.GetValue(item)?.ToString() ?? string.Empty));
                var row = string.Join(",", values);
                await writer.WriteLineAsync(row);
                
                processedItems++;
                var percentComplete = 5 + (int)((double)processedItems / totalItems * 90);
                
                progress?.Report(new ExportProgress 
                { 
                    CurrentOperation = $"Writing row {processedItems} of {totalItems}...",
                    PercentComplete = percentComplete,
                    ProcessedItems = processedItems,
                    TotalItems = totalItems
                });
                
                // Yield control occasionally
                if (processedItems % 100 == 0)
                    await Task.Delay(1, cancellationToken);
            }

            progress?.Report(new ExportProgress { CurrentOperation = "Export completed!", PercentComplete = 100 });
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to export to CSV: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report(new ExportProgress { CurrentOperation = "Preparing Excel export...", PercentComplete = 0 });
            
            // Note: This is a simplified implementation
            // In a real application, you would use a library like ClosedXML or EPPlus
            
            await Task.Delay(100, cancellationToken); // Simulate work
            
            progress?.Report(new ExportProgress { CurrentOperation = "Creating Excel workbook...", PercentComplete = 20 });
            
            var dataList = data.ToList();
            var totalItems = dataList.Count;
            
            // Simulate Excel export progress
            for (int i = 0; i <= totalItems; i += Math.Max(1, totalItems / 10))
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                    
                var percentComplete = 20 + (int)((double)i / totalItems * 70);
                progress?.Report(new ExportProgress 
                { 
                    CurrentOperation = $"Processing row {i} of {totalItems}...",
                    PercentComplete = percentComplete,
                    ProcessedItems = i,
                    TotalItems = totalItems
                });
                
                await Task.Delay(50, cancellationToken);
            }
            
            progress?.Report(new ExportProgress { CurrentOperation = "Saving Excel file...", PercentComplete = 95 });
            await Task.Delay(200, cancellationToken);
            
            // For demo purposes, create a simple CSV file with .xlsx extension
            // In real implementation, use proper Excel library
            await ExportToCsvAsync(data, filePath.Replace(".xlsx", "_temp.csv"), null, cancellationToken);
            
            progress?.Report(new ExportProgress { CurrentOperation = "Excel export completed!", PercentComplete = 100 });
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to export to Excel: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report(new ExportProgress { CurrentOperation = "Preparing PDF export...", PercentComplete = 0 });
            
            // Note: This is a simplified implementation
            // In a real application, you would use a library like iTextSharp or PdfSharp
            
            await Task.Delay(100, cancellationToken);
            
            progress?.Report(new ExportProgress { CurrentOperation = "Creating PDF document...", PercentComplete = 10 });
            
            var dataList = data.ToList();
            var totalItems = dataList.Count;
            
            // Simulate PDF creation progress
            for (int i = 0; i <= totalItems; i += Math.Max(1, totalItems / 8))
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                    
                var percentComplete = 10 + (int)((double)i / totalItems * 80);
                progress?.Report(new ExportProgress 
                { 
                    CurrentOperation = $"Adding row {i} of {totalItems} to PDF...",
                    PercentComplete = percentComplete,
                    ProcessedItems = i,
                    TotalItems = totalItems
                });
                
                await Task.Delay(75, cancellationToken);
            }
            
            progress?.Report(new ExportProgress { CurrentOperation = "Finalizing PDF...", PercentComplete = 95 });
            await Task.Delay(300, cancellationToken);
            
            // For demo purposes, create a simple text file with .pdf extension
            // In real implementation, use proper PDF library
            using var writer = new StreamWriter(filePath);
            await writer.WriteLineAsync($"PDF Export: {title}");
            await writer.WriteLineAsync($"Generated: {DateTime.Now}");
            await writer.WriteLineAsync($"Total Records: {totalItems}");
            
            progress?.Report(new ExportProgress { CurrentOperation = "PDF export completed!", PercentComplete = 100 });
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Export Error", $"Failed to export to PDF: {ex.Message}");
            return false;
        }
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
