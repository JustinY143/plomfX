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
        private double _currentScaleX = 1.0;
        private double _currentScaleY = 1.0;
        private bool _independentScaling = false;
        private double _currentOpacity = 1.0;
        private double _currentOffsetY = 0;
        private WpfColor _currentTint = Colors.White;
        private WinForms.ToolStripMenuItem? _toggleMenuItem;

        private System.Windows.Media.Imaging.BitmapSource? _currentOriginalBitmap;

        public MainWindow()
        {
            this.Closed += (s, e) => System.Windows.Application.Current.Shutdown();
            InitializeComponent();
            InitializeTrayIcon();
            _settings = SettingsService.Load();

            // Apply saved window size
            Width = _settings.MainWindowWidth;
            Height = _settings.MainWindowHeight;

            ActionMenuControl.SetDebugButtonVisibility(_settings.ShowDebugButton);

            // Create and configure the overlay window
            _overlayWindow = new OverlayWindow();
            _overlayWindow.Loaded += (s, e) =>
            {
                PositionOverlayOnMonitor(_settings.SelectedMonitorIndex);
            };

            // Load themes
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
            ActionMenuControl.DebugMemoryClick += OnDebugMemoryClick;

            // Settings popup events
            SettingsPopup.ScaleChanged += OnScaleChanged;
            SettingsPopup.OffsetYChanged += OnOffsetYChanged;
            SettingsPopup.OpacityChanged += OnOpacityChanged;
            SettingsPopup.TintChanged += OnTintChanged;
            SettingsPopup.BackRequested += OnBackRequested;

            // Load default crosshair if saved
            if (!string.IsNullOrEmpty(_settings.DefaultCrosshairPath) && File.Exists(_settings.DefaultCrosshairPath))
            {
                _currentCrosshairPath = _settings.DefaultCrosshairPath;
                _currentScaleX = _settings.DefaultScaleX;
                _currentScaleY = _settings.DefaultScaleY;
                _independentScaling = _settings.IndependentScaling;
                _currentOpacity = _settings.DefaultOpacity;
                _currentOffsetY = _settings.DefaultOffsetY;
                _currentTint = _settings.DefaultTint;

                _overlayWindow.SetCrosshairImage(_currentCrosshairPath);
                PreviewControl.SetPreviewImage(_currentCrosshairPath);

                _overlayWindow.SetScale(_currentScaleX, _currentScaleY);
                _overlayWindow.SetOpacity(_currentOpacity);
                _overlayWindow.SetColorTint(_currentTint);
                _overlayWindow.SetOffsetY(_currentOffsetY);
                PreviewControl.SetOpacity(_currentOpacity);
                PreviewControl.SetColorTint(_currentTint);

                SettingsPopup.SetInitialValues(_currentScaleX, _currentScaleY, _currentOpacity, _currentOffsetY, _currentTint, _independentScaling, _settings.UseSliderForOffset);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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

        public void RefreshOffsetInputMode()
        {
            var settings = SettingsService.Load();
            SettingsPopup.SetOffsetInputMode(settings.UseSliderForOffset);
        }

        private void OnColorTintClick(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.ColorDialog();
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
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

        private void OnBackRequested()
        {
            SettingsPopup.Visibility = Visibility.Collapsed;
            CrosshairBrowserControl.Visibility = Visibility.Visible;
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
                _overlayWindow.WindowState = WindowState.Normal;
                _overlayWindow.WindowStyle = WindowStyle.None;
                _overlayWindow.ResizeMode = ResizeMode.NoResize;
                _overlayWindow.Topmost = true;
            }
        }

        private void LoadAndShareBitmap(string imagePath)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                _currentOriginalBitmap = bitmap;

                _overlayWindow.SetOriginalBitmap(bitmap, imagePath);
                PreviewControl.SetOriginalBitmap(bitmap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load crosshair: {ex.Message}");
            }
        }

        private void OnCrosshairSelected(string imagePath)
        {
            _currentCrosshairPath = imagePath;
            LoadAndShareBitmap(imagePath);
            ApplyCrosshairProperties();
        }

        private void OnSetDefaultClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentCrosshairPath))
            {
                WinForms.MessageBox.Show("No crosshair selected to save.", "Save Crosshair");
                return;
            }

            _settings.DefaultCrosshairPath = _currentCrosshairPath;
            _settings.DefaultScaleX = _currentScaleX;
            _settings.DefaultScaleY = _currentScaleY;
            _settings.IndependentScaling = _independentScaling;
            _settings.DefaultOpacity = _currentOpacity;
            _settings.DefaultOffsetY = _currentOffsetY;
            _settings.DefaultTint = _currentTint;
            SettingsService.Save(_settings);
            WinForms.MessageBox.Show("Current crosshair saved as default.", "Save Crosshair");
        }

        // ---------- Customization Events ----------
        private void OnCrosshairSettingsClick(object sender, RoutedEventArgs e)
        {
            bool showSettings = SettingsPopup.Visibility != Visibility.Visible;
            SettingsPopup.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
            CrosshairBrowserControl.Visibility = showSettings ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnScaleChanged(double scaleX, double scaleY, bool independent)
        {
            _currentScaleX = scaleX;
            _currentScaleY = scaleY;
            _independentScaling = independent;
            _overlayWindow.SetScale(scaleX, scaleY);
            PreviewControl.SetScale(scaleX, scaleY);
        }

        private void OnOffsetYChanged(double offsetY)
        {
            _currentOffsetY = offsetY;
            _overlayWindow.SetOffsetY(offsetY);
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
            _overlayWindow.SetScale(_currentScaleX, _currentScaleY);
            _overlayWindow.SetOpacity(_currentOpacity);
            _overlayWindow.SetColorTint(_currentTint);
            _overlayWindow.SetOffsetY(_currentOffsetY);

            PreviewControl.SetScale(_currentScaleX, _currentScaleY);
            PreviewControl.SetOpacity(_currentOpacity);
            PreviewControl.SetColorTint(_currentTint);
        }

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

        private void OnDebugMemoryClick(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long managedMem = GC.GetTotalMemory(false);
            long workingSet = Environment.WorkingSet;
            System.Windows.MessageBox.Show($"Managed Memory: {managedMem / 1024 / 1024} MB\nWorking Set: {workingSet / 1024 / 1024} MB",
                            "Memory Usage");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _overlayWindow.Close();
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}