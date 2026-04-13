using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using plomfX.Services;
using plomfX.Views.UserControls;
using WpfColor = System.Windows.Media.Color;
using WinForms = System.Windows.Forms;

namespace plomfX.Views
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon? _notifyIcon;
        private OverlayWindow _overlayWindow;
        private AppSettings _settings;

        private string _currentCrosshairPath = string.Empty;
        private double _currentScale = 1.0; // 100%
        private double _currentOpacity = 1.0;
        private WpfColor _currentTint = WpfColor.FromRgb(255, 255, 255);
        private WinForms.ToolStripMenuItem? _toggleMenuItem;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            _settings = SettingsService.Load();
            _overlayWindow = new OverlayWindow();
            PositionOverlayOnMonitor(_settings.SelectedMonitorIndex);
            this.Closed += (s, e) => System.Windows.Application.Current.Shutdown();

            //Load themes
            var themes = ThemeManager.LoadThemes();
            ThemeComboBox.ItemsSource = themes;

            Theme? savedTheme = themes.FirstOrDefault(t => t.Name == _settings.ThemeName);
            if (savedTheme != null)
            {
                ThemeComboBox.SelectedItem = savedTheme;
                ThemeManager.ApplyTheme(savedTheme);
            }
            else if (themes.Count > 0)
            {
                ThemeComboBox.SelectedIndex = 0;
                ThemeManager.ApplyTheme(themes[0]);
            }

            ApplyMonitorSettings(); 

            // Wire up events
            ActionMenuControl.SettingsClick += OnSettingsClick;
            ActionMenuControl.ColorTintClick += OnColorTintClick;
            ActionMenuControl.CrosshairSettingsClick += OnCrosshairSettingsClick;
            ActionMenuControl.SetDefaultClick += OnSetDefaultClick;
            ActionMenuControl.EnableToggleChanged += OnEnableToggleChanged;
            CrosshairBrowserControl.CrosshairSelected += OnCrosshairSelected;


            // Settings popup events
            SettingsPopup.ScaleChanged += OnScaleChanged;
            SettingsPopup.OpacityChanged += OnOpacityChanged;
            SettingsPopup.TintChanged += OnTintChanged;

            // Load default crosshair if saved
            if (!string.IsNullOrEmpty(_settings.DefaultCrosshairPath) && File.Exists(_settings.DefaultCrosshairPath))
            {
                _currentCrosshairPath = _settings.DefaultCrosshairPath;
                _currentScale = _settings.DefaultScale;
                _currentOpacity = _settings.DefaultOpacity;
                _currentTint = _settings.DefaultTint;

                _overlayWindow.SetCrosshairImage(_currentCrosshairPath);
                PreviewControl.SetPreviewImage(_currentCrosshairPath);
                
                _overlayWindow.SetScale(_currentScale);
                _overlayWindow.SetOpacity(_currentOpacity);
                PreviewControl.SetOpacity(_currentOpacity);
                _overlayWindow.SetColorTint(_currentTint);
                PreviewControl.SetColorTint(_currentTint);
                
                SettingsPopup.SetInitialValues(_currentScale, _currentOpacity, _currentTint);
            }
        }

        // ---------- Tray ----------
        private void InitializeTrayIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Visible = false,
                Text = "plomfX Crosshair Overlay"
            };
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new WinForms.ContextMenuStrip();

            // Enable/Disable toggle item
            _toggleMenuItem = new WinForms.ToolStripMenuItem("Enable Crosshair");
            _toggleMenuItem.CheckOnClick = true;
            _toggleMenuItem.Checked = ActionMenuControl.IsOverlayEnabled;
            _toggleMenuItem.CheckedChanged += (s, e) =>
            {
                _toggleMenuItem.Text = _toggleMenuItem.Checked ? "Disable Crosshair" : "Enable Crosshair";
                ActionMenuControl.IsOverlayEnabled = _toggleMenuItem.Checked;
            };
            contextMenu.Items.Add(_toggleMenuItem);

            contextMenu.Items.Add(new WinForms.ToolStripSeparator());
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) => { _notifyIcon.Visible = false; System.Windows.Application.Current.Shutdown(); });
            
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Subscribe to overlay enabled changes to keep menu item in sync
            ActionMenuControl.EnableToggleChanged += (s, e) =>
            {
                if (_toggleMenuItem != null)
                    _toggleMenuItem.Checked = ActionMenuControl.IsOverlayEnabled;
            };
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            _notifyIcon!.Visible = false;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                _notifyIcon!.Visible = true;
            }
            base.OnStateChanged(e);
        }

        private void OnColorTintClick(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.ColorDialog();
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                // Convert System.Drawing.Color to System.Windows.Media.Color
                var winFormsColor = dialog.Color;
                var wpfColor = WpfColor.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
                _currentTint = wpfColor;
                _overlayWindow.SetColorTint(wpfColor);
            }
        }        

        // ---------- Theme ----------
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is Theme selectedTheme)
            {
                ThemeManager.ApplyTheme(selectedTheme);
                _settings.ThemeName = selectedTheme.Name;
                SettingsService.Save(_settings);
            }
        }

        // ---------- Action Menu Handlers ----------
        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        public void ApplyMonitorSettings()
        {
            var settings = SettingsService.Load();
            PositionOverlayOnMonitor(settings.SelectedMonitorIndex);
        }

        private void PositionOverlayOnMonitor(int monitorIndex)
        {
            var screens = WinForms.Screen.AllScreens;
            if (monitorIndex >= 0 && monitorIndex < screens.Length)
            {
                var screen = screens[monitorIndex];
                _overlayWindow.Left = screen.Bounds.Left;
                _overlayWindow.Top = screen.Bounds.Top;
                _overlayWindow.Width = screen.Bounds.Width;
                _overlayWindow.Height = screen.Bounds.Height;
                _overlayWindow.WindowState = WindowState.Normal; // Must be normal to set bounds
                _overlayWindow.WindowStyle = WindowStyle.None;
                _overlayWindow.ResizeMode = ResizeMode.NoResize;
                _overlayWindow.Topmost = true;
            }
        }

        private void OnCrosshairSettingsClick(object sender, RoutedEventArgs e)
        {
            bool showSettings = SettingsPopup.Visibility != Visibility.Visible;
            SettingsPopup.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
            CrosshairBrowserControl.Visibility = showSettings ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnSetDefaultClick(object sender, RoutedEventArgs e)
        {
            // Save current crosshair + settings as default
            if (string.IsNullOrEmpty(_currentCrosshairPath))
            {
                WinForms.MessageBox.Show("No crosshair selected to save.", "Save Crosshair");
                return;
            }

            _settings.DefaultCrosshairPath = _currentCrosshairPath;
            _settings.DefaultScale = _currentScale;
            _settings.DefaultOpacity = _currentOpacity;
            _settings.DefaultTint = _currentTint;
            SettingsService.Save(_settings);
            WinForms.MessageBox.Show("Current crosshair saved as default.", "Save Crosshair");
        }

        // ---------- Crosshair Selection ----------
        private void OnCrosshairSelected(string imagePath)
        {
            _currentCrosshairPath = imagePath;
            PreviewControl.SetPreviewImage(imagePath);
            _overlayWindow.SetCrosshairImage(imagePath);
            PreviewControl.SetOpacity(_currentOpacity);
            PreviewControl.SetColorTint(_currentTint);
            ApplyCrosshairProperties();
        }

        // ---------- Customization Events ----------
        private void OnScaleChanged(double scale)
        {
            _currentScale = scale;
            _overlayWindow.SetScale(scale);
        }

        private void OnOpacityChanged(double opacity)
        {
            _currentOpacity = opacity;
            _overlayWindow.SetOpacity(opacity);
            PreviewControl.SetOpacity(opacity);
        }

        private void OnTintChanged(WpfColor color)
        {
            _currentTint = color;
            _overlayWindow.SetColorTint(color);
            PreviewControl.SetColorTint(color);
        }
        private void ApplyCrosshairProperties()
        {
            _overlayWindow.SetScale(_currentScale);
            _overlayWindow.SetOpacity(_currentOpacity);
            _overlayWindow.SetColorTint(_currentTint);
        }


        // Modify OnEnableToggleChanged to use the helper
        private void OnEnableToggleChanged(object sender, RoutedEventArgs e)
        {
            if (ActionMenuControl.IsOverlayEnabled)
            {
                _overlayWindow.Show();
                ApplyCrosshairProperties();
            }
            else
            {
                _overlayWindow.Hide();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _overlayWindow.Close();
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}