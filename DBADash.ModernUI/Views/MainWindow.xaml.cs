using Microsoft.UI.Xaml.Controls;
using DBADash.ModernUI.ViewModels;
using Microsoft.UI.Xaml;

namespace DBADash.ModernUI.Views;

/// <summary>
/// Main window using WinUI 3 with modern navigation
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Set up modern window properties
        Title = "DBA Dash - Modern UI";
        
        // Initialize ViewModel
        ViewModel = App.GetService<MainViewModel>();
        DataContext = ViewModel;
        
        // Configure window appearance
        SetupWindowAppearance();
        
        // Initialize navigation
        ViewModel.NavigationService.Initialize(ContentFrame);
    }

    private void SetupWindowAppearance()
    {
        // Set minimum window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
        
        // Enable backdrop effect for modern look
        this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        
        // Configure title bar
        var titleBar = this.AppWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                ViewModel.NavigateToPage(tag);
            }
        }
    }

    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        ViewModel.NavigationService.GoBack();
    }
}
