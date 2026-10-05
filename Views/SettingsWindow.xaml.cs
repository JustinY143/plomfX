using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using plomfX.Services;
using WinForms = System.Windows.Forms;
using System.Windows.Input;

namespace plomfX.Views
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private List<ScreenInfo> _screens = new();
        private ModifierKeys _pendingModifiers = ModifierKeys.None;
        private Key _pendingKey = Key.None;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = SettingsService.Load();

            if (_settings.HotkeyEnabled && _settings.HotkeyKey != 0) {
                var mods = (ModifierKeys)_settings.HotkeyModifiers;
                var key = KeyInterop.KeyFromVirtualKey(_settings.HotkeyKey);

                var parts = new System.Collections.Generic.List<string>();
                if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
                if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
                if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
                if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
                parts.Add(KeyToDisplay(key));

                HotkeyTextBox.Text = string.Join(" + ", parts);
                _pendingModifiers = mods;
                _pendingKey = key;
            }

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
                _settings.SelectedMonitorIndex = selected.Index;

            _settings.UseSliderForOffset = SliderOffsetCheckBox.IsChecked == true;

            // Hotkey
            if (_pendingKey != Key.None && _pendingModifiers != ModifierKeys.None)
            {
                _settings.HotkeyEnabled = true;
                _settings.HotkeyModifiers = (int)_pendingModifiers;
                _settings.HotkeyKey = KeyInterop.VirtualKeyFromKey(_pendingKey);
            }
            else
            {
                _settings.HotkeyEnabled = false;
                _settings.HotkeyKey = 0;
            }

            SettingsService.Save(_settings);

            if (Owner is MainWindow mw)
            {
                mw.ApplyMonitorSettings();
                mw.RefreshOffsetInputMode();
                mw.RefreshHotkey();
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
        private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HotkeyTextBox.Text = "Press a key combo...";
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore lone modifier keys — wait for a "real" key
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // Ignore pure arrow keys as they conflict with user expectations
            if (key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right)
                return;

            var mods = Keyboard.Modifiers;

            // Require at least one modifier to avoid hijacking plain keys
            if (mods == ModifierKeys.None)
            {
                HotkeyTextBox.Text = "Add Ctrl/Alt/Shift/Win";
                return;
            }

            _pendingModifiers = mods;
            _pendingKey = key;

            var parts = new System.Collections.Generic.List<string>();
            if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            parts.Add(KeyToDisplay(key));

            HotkeyTextBox.Text = string.Join(" + ", parts);
        }

        private string KeyToDisplay(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9)
                return ((int)(key - Key.D0)).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return "Num" + ((int)(key - Key.NumPad0));
            return key.ToString();
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            _pendingModifiers = ModifierKeys.None;
            _pendingKey = Key.None;
            HotkeyTextBox.Text = "Click to record...";
        }

        private void ResetWindowSize_Click(object sender, RoutedEventArgs e)
        {
            _settings.MainWindowWidth = 800;
            _settings.MainWindowHeight = 450;
            SettingsService.Save(_settings);

            if (Owner is MainWindow mw)
            {
                mw.Width = 800;
                mw.Height = 450;
            }
            WindowSizeText.Text = "(800 × 450)";
        }

    }
}