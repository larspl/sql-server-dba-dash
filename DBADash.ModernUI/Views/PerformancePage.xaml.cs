using Microsoft.UI.Xaml.Controls;
using DBADash.ModernUI.ViewModels;

namespace DBADash.ModernUI.Views;

public sealed partial class PerformancePage : Page
{
    public PerformanceViewModel ViewModel { get; }

    public PerformancePage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<PerformanceViewModel>();
        this.DataContext = ViewModel;
    }

    private async void CpuTimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.RefreshDataAsync();
        }
    }

    private async void MemoryTimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.RefreshDataAsync();
        }
    }

    private async void DiskTimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.RefreshDataAsync();
        }
    }

    private async void ConnectionTimeRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.RefreshDataAsync();
        }
    }
}
