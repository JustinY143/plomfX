using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using plomfX.Services;

namespace plomfX.Views
{
    public partial class ColorPickerDialog : Window
    {
        public WpfColor SelectedColor { get; private set; } = Colors.White;
        private AppSettings _settings;
        private List<WpfColor> _customColors = new();

        public ColorPickerDialog(WpfColor initialColor)
        {
            InitializeComponent();
            _settings = SettingsService.Load();
            LoadCustomColors();

            SelectedColor = initialColor;
            RedSlider.Value = initialColor.R;
            GreenSlider.Value = initialColor.G;
            BlueSlider.Value = initialColor.B;
            UpdateHexFromColor();
            UpdatePreview();

            // Wire events
            RedSlider.ValueChanged += (s, e) => { RedTextBox.Text = ((int)e.NewValue).ToString(); UpdateFromSliders(); };
            GreenSlider.ValueChanged += (s, e) => { GreenTextBox.Text = ((int)e.NewValue).ToString(); UpdateFromSliders(); };
            BlueSlider.ValueChanged += (s, e) => { BlueTextBox.Text = ((int)e.NewValue).ToString(); UpdateFromSliders(); };

            RedTextBox.TextChanged += (s, e) => OnTextBoxChanged(RedTextBox, RedSlider);
            GreenTextBox.TextChanged += (s, e) => OnTextBoxChanged(GreenTextBox, GreenSlider);
            BlueTextBox.TextChanged += (s, e) => OnTextBoxChanged(BlueTextBox, BlueSlider);

            RedTextBox.LostFocus += (s, e) => ValidateTextBox(RedTextBox, RedSlider);
            GreenTextBox.LostFocus += (s, e) => ValidateTextBox(GreenTextBox, GreenSlider);
            BlueTextBox.LostFocus += (s, e) => ValidateTextBox(BlueTextBox, BlueSlider);
        }

        private void LoadCustomColors()
        {
            _customColors = _settings.CustomColors
                .Select(hex => ParseHexString(hex) ?? Colors.White)
                .ToList();
            RefreshPalette();
        }

        private void RefreshPalette()
        {
            CustomColorsPanel.Children.Clear();
            foreach (var color in _customColors)
            {
                var swatch = CreateColorSwatch(color);
                CustomColorsPanel.Children.Add(swatch);
            }
            // Add the "+" button at the end
            var addButton = new System.Windows.Controls.Button
            {
                Content = "+",
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Style = (Style)FindResource("ActionButtonStyle"),
                Tag = "Add"
            };
            addButton.Click += AddCustomColor_Click;
            CustomColorsPanel.Children.Add(addButton);
        }

        private Border CreateColorSwatch(WpfColor color)
        {
            var border = new Border
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(color),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = color
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Border b && b.Tag is WpfColor c)
                {
                    SetColor(c);
                }
            };
            border.MouseRightButtonDown += (s, e) =>
            {
                if (s is Border b && b.Tag is WpfColor c)
                {
                    _customColors.Remove(c);
                    SaveCustomColors();
                    RefreshPalette();
                }
            };
            return border;
        }

        private void SetColor(WpfColor color)
        {
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
            UpdateHexFromColor();
            UpdatePreview();
        }

        private void AddCustomColor_Click(object sender, RoutedEventArgs e)
        {
            var current = WpfColor.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            if (!_customColors.Contains(current))
            {
                _customColors.Add(current);
                SaveCustomColors();
                RefreshPalette();
            }
        }

        private void SaveCustomColors()
        {
            _settings.CustomColors = _customColors.Select(c => c.ToString()).ToList();
            SettingsService.Save(_settings);
        }

        private void UpdateFromSliders()
        {
            UpdateHexFromColor();
            UpdatePreview();
        }

        private void UpdateHexFromColor()
        {
            var color = WpfColor.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            HexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void UpdatePreview()
        {
            var color = WpfColor.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            ColorPreviewBorder.Background = new SolidColorBrush(color);
            SelectedColor = color;
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var color = ParseHexString(HexTextBox.Text);
            if (color.HasValue)
            {
                RedSlider.Value = color.Value.R;
                GreenSlider.Value = color.Value.G;
                BlueSlider.Value = color.Value.B;
                UpdatePreview();
            }
        }

        private WpfColor? ParseHexString(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return null;

            // Remove leading # and trim
            hex = hex.Trim().TrimStart('#');

            if (hex.Length == 0)
                return null;

            // Expand 3-digit shorthand (#RGB -> #RRGGBB)
            if (hex.Length == 3)
            {
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            }
            else if (hex.Length == 4) // #RGBA? Not standard, ignore alpha
            {
                hex = hex.Substring(0, 3);
                hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
            }
            else if (hex.Length == 6)
            {
                // Standard RRGGBB
            }
            else if (hex.Length == 8)
            {
                // Assume AARRGGBB, strip alpha
                hex = hex.Substring(2);
            }
            else
            {
                return null;
            }

            try
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return WpfColor.FromRgb(r, g, b);
            }
            catch
            {
                return null;
            }
        }

        private void OnTextBoxChanged(System.Windows.Controls.TextBox textBox, Slider slider)
        {
            if (int.TryParse(textBox.Text, out int value))
            {
                value = Math.Clamp(value, 0, 255);
                if (Math.Abs(slider.Value - value) > 0.01)
                    slider.Value = value;
            }
        }

        private void ValidateTextBox(System.Windows.Controls.TextBox textBox, Slider slider)
        {
            if (!int.TryParse(textBox.Text, out int value))
                value = (int)slider.Value;
            value = Math.Clamp(value, 0, 255);
            textBox.Text = value.ToString();
            slider.Value = value;
            UpdatePreview();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}