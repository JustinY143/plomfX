using System.Windows;

namespace plomfX;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Environment.Exit(0);
    }
}

