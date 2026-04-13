using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms; // For Screen
using plomfX.Services;

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
        }

        private void LoadMonitors()
        {
            _screens = Screen.AllScreens.Select((s, i) => new ScreenInfo
            {
                DeviceName = s.DeviceName,
                Bounds = s.Bounds,
                IsPrimary = s.Primary,
                Index = i
            }).ToList();

            MonitorComboBox.ItemsSource = _screens;

            // Select the saved monitor index, default to primary
            int savedIndex = _settings.SelectedMonitorIndex;
            if (savedIndex >= 0 && savedIndex < _screens.Count)
                MonitorComboBox.SelectedIndex = savedIndex;
            else
                MonitorComboBox.SelectedIndex = _screens.FindIndex(s => s.IsPrimary);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (MonitorComboBox.SelectedItem is ScreenInfo selected)
            {
                _settings.SelectedMonitorIndex = selected.Index;
                SettingsService.Save(_settings);
                // Notify MainWindow to reposition overlay
                ((MainWindow)Owner).ApplyMonitorSettings();
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