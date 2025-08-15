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
        DataContext = ViewModel;
    }
}

public sealed partial class InstancesPage : Page
{
    public InstancesViewModel ViewModel { get; }

    public InstancesPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<InstancesViewModel>();
        DataContext = ViewModel;
    }
}

public sealed partial class AlertsPage : Page
{
    public AlertsViewModel ViewModel { get; }

    public AlertsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<AlertsViewModel>();
        DataContext = ViewModel;
    }
}

public sealed partial class JobsPage : Page
{
    public JobsViewModel ViewModel { get; }

    public JobsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<JobsViewModel>();
        DataContext = ViewModel;
    }
}

public sealed partial class BackupsPage : Page
{
    public BackupsViewModel ViewModel { get; }

    public BackupsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<BackupsViewModel>();
        DataContext = ViewModel;
    }
}

public sealed partial class QueriesPage : Page
{
    public QueriesViewModel ViewModel { get; }

    public QueriesPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<QueriesViewModel>();
        DataContext = ViewModel;
    }
}

public sealed partial class ReportsPage : Page
{
    public ReportsPage()
    {
        this.InitializeComponent();
    }
}

public sealed partial class ConfigurationPage : Page
{
    public ConfigurationPage()
    {
        this.InitializeComponent();
    }
}

public sealed partial class HelpPage : Page
{
    public HelpPage()
    {
        this.InitializeComponent();
    }
}

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        this.InitializeComponent();
    }
}
