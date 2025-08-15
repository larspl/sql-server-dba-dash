using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DBADash.ModernUI.Services;

namespace DBADash.ModernUI.ViewModels;

public partial class PerformanceViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    
    [ObservableProperty]
    private string title = "Performance Monitoring";
    
    [ObservableProperty]
    private string cpuUsage = "75.3%";
    
    [ObservableProperty]
    private string cpuTrend = "↑ 2.1% from last hour";
    
    [ObservableProperty]
    private string memoryUsage = "89.7%";
    
    [ObservableProperty]
    private string memoryTrend = "↓ 1.2% from last hour";
    
    [ObservableProperty]
    private string diskIO = "45.2 MB/s";
    
    [ObservableProperty]
    private string diskTrend = "Normal activity";
    
    [ObservableProperty]
    private string activeConnections = "234";
    
    [ObservableProperty]
    private string connectionTrend = "12 new connections";

    public ISeries[] CpuSeries { get; set; }
    public ISeries[] MemorySeries { get; set; }
    public ISeries[] DiskSeries { get; set; }
    public ISeries[] ConnectionSeries { get; set; }

    public PerformanceViewModel(IDataService dataService)
    {
        _dataService = dataService;
        InitializeCharts();
        _ = LoadPerformanceDataAsync();
    }

    private void InitializeCharts()
    {
        // CPU Chart
        CpuSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = GenerateSampleData(60), // 60 data points
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Name = "CPU %"
            }
        };

        // Memory Chart
        MemorySeries = new ISeries[]
        {
            new AreaSeries<double>
            {
                Values = GenerateSampleData(60, 80, 95), // Higher baseline for memory
                Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(100)),
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 2 },
                GeometrySize = 0,
                Name = "Memory %"
            }
        };

        // Disk I/O Chart (Read & Write)
        DiskSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = GenerateSampleData(60, 20, 60),
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Name = "Disk Read MB/s"
            },
            new LineSeries<double>
            {
                Values = GenerateSampleData(60, 15, 45),
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Name = "Disk Write MB/s"
            }
        };

        // Connection Chart
        ConnectionSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = GenerateSampleData(24, 180, 280), // 24 hours of data
                Fill = new SolidColorPaint(SKColors.DeepSkyBlue),
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 1 },
                Name = "Active Connections"
            }
        };
    }

    private double[] GenerateSampleData(int count, double min = 50, double max = 100)
    {
        var random = new Random();
        var data = new double[count];
        var baseValue = (min + max) / 2;
        
        for (int i = 0; i < count; i++)
        {
            // Create realistic variation with some trending
            var variation = (random.NextDouble() - 0.5) * 20; // ±10% variation
            var trend = Math.Sin(i * 0.1) * 5; // Subtle sine wave trend
            
            data[i] = Math.Max(min, Math.Min(max, baseValue + variation + trend));
        }
        
        return data;
    }

    public async Task LoadPerformanceDataAsync()
    {
        try
        {
            // In a real implementation, this would load actual performance data
            await Task.Delay(100); // Simulate async operation
            
            // Update current metrics (these would come from real data)
            var random = new Random();
            CpuUsage = $"{75 + random.Next(-5, 5)}.{random.Next(0, 9)}%";
            MemoryUsage = $"{87 + random.Next(-3, 8)}.{random.Next(0, 9)}%";
            DiskIO = $"{40 + random.Next(-10, 20)}.{random.Next(0, 9)} MB/s";
            ActiveConnections = $"{220 + random.Next(-20, 40)}";
            
            // Update trends
            CpuTrend = random.Next(0, 2) == 0 ? "↑ 2.1% from last hour" : "↓ 1.3% from last hour";
            MemoryTrend = random.Next(0, 2) == 0 ? "↑ 1.8% from last hour" : "↓ 0.9% from last hour";
            DiskTrend = "Normal activity";
            ConnectionTrend = $"{random.Next(5, 25)} new connections";
        }
        catch (Exception ex)
        {
            // Log error and show fallback data
            System.Diagnostics.Debug.WriteLine($"Error loading performance data: {ex.Message}");
        }
    }

    public async Task RefreshDataAsync()
    {
        await LoadPerformanceDataAsync();
        
        // In a real implementation, update chart data here
        // For now, regenerate sample data
        UpdateChartData();
    }

    private void UpdateChartData()
    {
        // Update CPU data
        if (CpuSeries[0] is LineSeries<double> cpuSeries)
        {
            cpuSeries.Values = GenerateSampleData(60);
        }

        // Update Memory data
        if (MemorySeries[0] is AreaSeries<double> memorySeries)
        {
            memorySeries.Values = GenerateSampleData(60, 80, 95);
        }

        // Update Disk data
        if (DiskSeries[0] is LineSeries<double> diskReadSeries && 
            DiskSeries[1] is LineSeries<double> diskWriteSeries)
        {
            diskReadSeries.Values = GenerateSampleData(60, 20, 60);
            diskWriteSeries.Values = GenerateSampleData(60, 15, 45);
        }

        // Update Connection data
        if (ConnectionSeries[0] is ColumnSeries<double> connectionSeries)
        {
            connectionSeries.Values = GenerateSampleData(24, 180, 280);
        }
    }
}
