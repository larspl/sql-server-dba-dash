using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using LiveChartsCore;
using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace DBADash.ModernUI.Controls;

public sealed partial class DatabaseMetricsWidget : UserControl
{
    public DatabaseMetricsWidget()
    {
        this.InitializeComponent();
        RefreshCommand = new RelayCommand(OnRefresh);
    }

    #region Dependency Properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata("\uE9F3"));

    public static readonly DependencyProperty MainValueProperty =
        DependencyProperty.Register(nameof(MainValue), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MainUnitProperty =
        DependencyProperty.Register(nameof(MainUnit), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TrendTextProperty =
        DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata("OK"));

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowChartProperty =
        DependencyProperty.Register(nameof(ShowChart), typeof(bool), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(false));

    public static readonly DependencyProperty ChartSeriesProperty =
        DependencyProperty.Register(nameof(ChartSeries), typeof(ISeries[]), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(null));

    public static readonly DependencyProperty ValueColorProperty =
        DependencyProperty.Register(nameof(ValueColor), typeof(Brush), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(null));

    public static readonly DependencyProperty TrendColorProperty =
        DependencyProperty.Register(nameof(TrendColor), typeof(Brush), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(null));

    public static readonly DependencyProperty LastUpdatedProperty =
        DependencyProperty.Register(nameof(LastUpdated), typeof(DateTime), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(DateTime.Now));

    public static readonly DependencyProperty HasAlertProperty =
        DependencyProperty.Register(nameof(HasAlert), typeof(bool), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(false));

    public static readonly DependencyProperty AlertCountProperty =
        DependencyProperty.Register(nameof(AlertCount), typeof(int), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(0));

    public static readonly DependencyProperty AlertSeverityProperty =
        DependencyProperty.Register(nameof(AlertSeverity), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata("Info"));

    public static readonly DependencyProperty ThresholdStatusProperty =
        DependencyProperty.Register(nameof(ThresholdStatus), typeof(string), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata("Normal"));

    public static readonly DependencyProperty ThresholdColorProperty =
        DependencyProperty.Register(nameof(ThresholdColor), typeof(Brush), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(null));

    public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(DatabaseMetricsWidget), 
            new PropertyMetadata(null));

    #endregion

    #region Properties

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string MainValue
    {
        get => (string)GetValue(MainValueProperty);
        set => SetValue(MainValueProperty, value);
    }

    public string MainUnit
    {
        get => (string)GetValue(MainUnitProperty);
        set => SetValue(MainUnitProperty, value);
    }

    public string TrendText
    {
        get => (string)GetValue(TrendTextProperty);
        set => SetValue(TrendTextProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool ShowChart
    {
        get => (bool)GetValue(ShowChartProperty);
        set => SetValue(ShowChartProperty, value);
    }

    public ISeries[] ChartSeries
    {
        get => (ISeries[])GetValue(ChartSeriesProperty);
        set => SetValue(ChartSeriesProperty, value);
    }

    public Brush ValueColor
    {
        get => (Brush)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public Brush TrendColor
    {
        get => (Brush)GetValue(TrendColorProperty);
        set => SetValue(TrendColorProperty, value);
    }

    public DateTime LastUpdated
    {
        get => (DateTime)GetValue(LastUpdatedProperty);
        set => SetValue(LastUpdatedProperty, value);
    }

    public bool HasAlert
    {
        get => (bool)GetValue(HasAlertProperty);
        set => SetValue(HasAlertProperty, value);
    }

    public int AlertCount
    {
        get => (int)GetValue(AlertCountProperty);
        set => SetValue(AlertCountProperty, value);
    }

    public string AlertSeverity
    {
        get => (string)GetValue(AlertSeverityProperty);
        set => SetValue(AlertSeverityProperty, value);
    }

    public string ThresholdStatus
    {
        get => (string)GetValue(ThresholdStatusProperty);
        set => SetValue(ThresholdStatusProperty, value);
    }

    public Brush ThresholdColor
    {
        get => (Brush)GetValue(ThresholdColorProperty);
        set => SetValue(ThresholdColorProperty, value);
    }

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<EventArgs>? RefreshRequested;

    #endregion

    #region Methods

    private void OnRefresh()
    {
        IsLoading = true;
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetMetricValue(double value, string unit, string trend = "", bool isGood = true)
    {
        MainValue = value.ToString("F1");
        MainUnit = unit;
        TrendText = trend;
        
        // Set colors based on whether the metric is good or bad
        var resources = Application.Current.Resources;
        ValueColor = isGood ? 
            (Brush)resources["SystemFillColorSuccessBrush"] : 
            (Brush)resources["SystemFillColorCriticalBrush"];
            
        TrendColor = trend.StartsWith("↑") ? 
            (isGood ? (Brush)resources["SystemFillColorSuccessBrush"] : (Brush)resources["SystemFillColorCriticalBrush"]) :
            (isGood ? (Brush)resources["SystemFillColorCriticalBrush"] : (Brush)resources["SystemFillColorSuccessBrush"]);
        
        LastUpdated = DateTime.Now;
        IsLoading = false;
    }

    public void SetAlert(int count, string severity)
    {
        HasAlert = count > 0;
        AlertCount = count;
        AlertSeverity = severity;
    }

    public void SetThreshold(string status, bool isWithinThreshold)
    {
        ThresholdStatus = status;
        var resources = Application.Current.Resources;
        ThresholdColor = isWithinThreshold ? 
            (Brush)resources["SystemFillColorSuccessBrush"] : 
            (Brush)resources["SystemFillColorCautionBrush"];
    }

    #endregion
}
