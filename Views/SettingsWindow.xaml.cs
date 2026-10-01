using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using plomfX.Services;
using WinForms = System.Windows.Forms;

namespace plomfX.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private List<ScreenInfo> _screens = new();

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = SettingsService.Load();

            LoadMonitors();

            // Load offset input style
            SliderOffsetCheckBox.IsChecked = _settings.UseSliderForOffset;

            // Show current main window size
            if (Owner is MainWindow mw)
            {
                WindowSizeText.Text = $"({(int)mw.Width} × {(int)mw.Height})";
            }
        }

        private void LoadMonitors()
        {
            _screens = WinForms.Screen.AllScreens.Select((s, i) => new ScreenInfo
            {
                DeviceName = s.DeviceName,
                Bounds = s.Bounds,
                IsPrimary = s.Primary,
                Index = i
            }).ToList();

            MonitorComboBox.ItemsSource = _screens;

            int savedIndex = _settings.SelectedMonitorIndex;
            if (savedIndex >= 0 && savedIndex < _screens.Count)
                MonitorComboBox.SelectedIndex = savedIndex;
            else
                MonitorComboBox.SelectedIndex = _screens.FindIndex(s => s.IsPrimary);
        }

        private void SaveWindowSize_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow mw)
            {
                _settings.MainWindowWidth = mw.Width;
                _settings.MainWindowHeight = mw.Height;
                SettingsService.Save(_settings);
                WindowSizeText.Text = $"({(int)mw.Width} × {(int)mw.Height})";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (MonitorComboBox.SelectedItem is ScreenInfo selected)
            {
                _settings.SelectedMonitorIndex = selected.Index;
            }

            _settings.UseSliderForOffset = SliderOffsetCheckBox.IsChecked == true;

            SettingsService.Save(_settings);

            // Notify MainWindow to reposition overlay and refresh settings
            if (Owner is MainWindow mw)
            {
                mw.ApplyMonitorSettings();
                mw.RefreshOffsetInputMode();
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public class ScreenInfo
        {
            public string DeviceName { get; set; } = string.Empty;
            public System.Drawing.Rectangle Bounds { get; set; }
            public bool IsPrimary { get; set; }
            public int Index { get; set; }
        }
    }
}