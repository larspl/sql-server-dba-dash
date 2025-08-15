using Microsoft.UI.Xaml.Controls;
using DBADash.ModernUI.ViewModels;

namespace DBADash.ModernUI.Views;

/// <summary>
/// Summary page showing overview of all monitored instances
/// </summary>
public sealed partial class SummaryPage : Page
{
    public SummaryViewModel ViewModel { get; }

    public SummaryPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SummaryViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
    }
}
