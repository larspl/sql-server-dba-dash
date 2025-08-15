using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace DBADash.ModernUI.Services;

/// <summary>
/// Dialog service interface for showing dialogs and messages
/// </summary>
public interface IDialogService
{
    Task ShowInfoAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string message, string placeholder = "");
}

/// <summary>
/// Dialog service implementation using WinUI 3 ContentDialog
/// </summary>
public class DialogService : IDialogService
{
    public async Task ShowInfoAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task ShowWarningAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<string?> ShowInputAsync(string title, string message, string placeholder = "")
    {
        var textBox = new TextBox
        {
            PlaceholderText = placeholder,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0)
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(new TextBlock { Text = message });
        stackPanel.Children.Add(textBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stackPanel,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }

    private static Microsoft.UI.Xaml.XamlRoot? GetXamlRoot()
    {
        return Microsoft.UI.Xaml.Application.Current.MainWindow?.Content?.XamlRoot;
    }
}
