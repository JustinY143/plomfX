using System.Windows;

namespace plomfX;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            System.Windows.MessageBox.Show($"UI Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                System.Windows.MessageBox.Show($"Fatal Exception: {ex.Message}\n\n{ex.StackTrace}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            Environment.Exit(1);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Environment.Exit(0);
    }
}

