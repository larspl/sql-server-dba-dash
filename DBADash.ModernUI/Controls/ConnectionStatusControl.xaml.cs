using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DBADash.ModernUI.ViewModels;

namespace DBADash.ModernUI.Controls;

/// <summary>
/// Connection status indicator control
/// </summary>
public sealed partial class ConnectionStatusControl : UserControl
{
    public ConnectionStatusViewModel? ViewModel => DataContext as ConnectionStatusViewModel;

    public ConnectionStatusControl()
    {
        this.InitializeComponent();
    }

    public Brush GetStatusColor(bool isConnected)
    {
        return isConnected 
            ? new SolidColorBrush(Microsoft.UI.Colors.Green)
            : new SolidColorBrush(Microsoft.UI.Colors.Red);
    }
}
