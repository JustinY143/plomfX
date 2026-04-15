using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views
{
    public partial class ColorPickerDialog : Window
    {
        public WpfColor SelectedColor { get; private set; } = Colors.White;

        public ColorPickerDialog(WpfColor initialColor)
        {
            InitializeComponent();
            SelectedColor = initialColor;
            RedSlider.Value = initialColor.R;
            GreenSlider.Value = initialColor.G;
            BlueSlider.Value = initialColor.B;
            UpdateTextBoxes();
            UpdatePreview();

            // Wire events
            RedSlider.ValueChanged += (s, e) => { RedTextBox.Text = ((int)e.NewValue).ToString(); UpdatePreview(); };
            GreenSlider.ValueChanged += (s, e) => { GreenTextBox.Text = ((int)e.NewValue).ToString(); UpdatePreview(); };
            BlueSlider.ValueChanged += (s, e) => { BlueTextBox.Text = ((int)e.NewValue).ToString(); UpdatePreview(); };

            RedTextBox.TextChanged += (s, e) => OnTextBoxChanged(RedTextBox, RedSlider);
            GreenTextBox.TextChanged += (s, e) => OnTextBoxChanged(GreenTextBox, GreenSlider);
            BlueTextBox.TextChanged += (s, e) => OnTextBoxChanged(BlueTextBox, BlueSlider);

            RedTextBox.LostFocus += (s, e) => ValidateTextBox(RedTextBox, RedSlider);
            GreenTextBox.LostFocus += (s, e) => ValidateTextBox(GreenTextBox, GreenSlider);
            BlueTextBox.LostFocus += (s, e) => ValidateTextBox(BlueTextBox, BlueSlider);
        }

        private void OnTextBoxChanged(System.Windows.Controls.TextBox textBox, Slider slider)
        {
            if (int.TryParse(textBox.Text, out int value))
            {
                value = Math.Clamp(value, 0, 255);
                if (slider.Value != value)
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

        private void UpdateTextBoxes()
        {
            RedTextBox.Text = ((int)RedSlider.Value).ToString();
            GreenTextBox.Text = ((int)GreenSlider.Value).ToString();
            BlueTextBox.Text = ((int)BlueSlider.Value).ToString();
        }

        private void UpdatePreview()
        {
            var color = WpfColor.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            ColorPreviewBorder.Background = new SolidColorBrush(color);
            SelectedColor = color;
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