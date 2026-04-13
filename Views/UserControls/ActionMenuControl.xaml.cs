using System.Windows;
using System.Windows.Controls;

namespace plomfX.Views.UserControls
{
    public partial class ActionMenuControl : System.Windows.Controls.UserControl
    {
        // Event handlers will be wired in MainWindow or via commands later
        public event RoutedEventHandler SettingsClick = delegate { };
        public event RoutedEventHandler ColorTintClick = delegate { };
        public event RoutedEventHandler CrosshairSettingsClick = delegate { };
        public event RoutedEventHandler SetDefaultClick = delegate { };
        public event RoutedEventHandler EnableToggleChanged = delegate { };

        public ActionMenuControl()
        {
            InitializeComponent();
            
            SettingsButton.Click += (s, e) => SettingsClick?.Invoke(this, e);
            CrosshairSettingsButton.Click += (s, e) => CrosshairSettingsClick?.Invoke(this, e);
            SetDefaultButton.Click += (s, e) => SetDefaultClick?.Invoke(this, e);
            EnableToggleButton.Checked += (s, e) => EnableToggleChanged?.Invoke(this, e);
            EnableToggleButton.Unchecked += (s, e) => EnableToggleChanged?.Invoke(this, e);
        }

        public bool IsOverlayEnabled
        {
            get => EnableToggleButton.IsChecked ?? false;
            set => EnableToggleButton.IsChecked = value;
        }
    }
}