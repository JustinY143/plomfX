using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views.UserControls
{
    public partial class CrosshairSettingsControl : System.Windows.Controls.UserControl
    {
        // 2-parameter ScaleChanged
        public event Action<double, double>? ScaleChanged;
        public event Action<double>? OpacityChanged;
        public event Action<WpfColor>? TintChanged;
        public event Action? BackRequested;

        private WpfColor _currentTint = Colors.White;
        private bool _independentScale = false;

        public CrosshairSettingsControl()
        {
            InitializeComponent();

            ScaleSlider.ValueChanged += (s, e) =>
            {
                ScaleValueText.Text = $"{e.NewValue:P0}";
                if (!_independentScale)
                    ScaleChanged?.Invoke(e.NewValue, e.NewValue);
            };

            ScaleXSlider.ValueChanged += (s, e) =>
            {
                ScaleXValueText.Text = $"{e.NewValue:P0}";
                if (_independentScale)
                    ScaleChanged?.Invoke(e.NewValue, ScaleYSlider.Value);
            };

            ScaleYSlider.ValueChanged += (s, e) =>
            {
                ScaleYValueText.Text = $"{e.NewValue:P0}";
                if (_independentScale)
                    ScaleChanged?.Invoke(ScaleXSlider.Value, e.NewValue);
            };

            OpacitySlider.ValueChanged += (s, e) =>
            {
                OpacityValueText.Text = $"{e.NewValue:P0}";
                OpacityChanged?.Invoke(e.NewValue);
            };

            PickColorButton.Click += (s, e) =>
            {
                var dialog = new Views.ColorPickerDialog(_currentTint);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    _currentTint = dialog.SelectedColor;
                    ColorPreview.Fill = new SolidColorBrush(_currentTint);
                    TintChanged?.Invoke(_currentTint);
                }
            };

            ResetColorButton.Click += (s, e) =>
            {
                _currentTint = Colors.White;
                ColorPreview.Fill = new SolidColorBrush(Colors.White);
                TintChanged?.Invoke(Colors.White);
            };

            BackButton.Click += (s, e) => BackRequested?.Invoke();
        }

        // Matches the XAML Checked/Unchecked handlers
        private void IndependentScale_Changed(object sender, RoutedEventArgs e)
        {
            _independentScale = IndependentScaleCheckBox.IsChecked == true;

            if (_independentScale)
            {
                ScaleXSlider.Value = ScaleSlider.Value;
                ScaleYSlider.Value = ScaleSlider.Value;
                UnifiedScalePanel.Visibility = Visibility.Collapsed;
                IndependentScalePanel.Visibility = Visibility.Visible;
                ScaleChanged?.Invoke(ScaleXSlider.Value, ScaleYSlider.Value);
            }
            else
            {
                ScaleSlider.Value = ScaleXSlider.Value;
                UnifiedScalePanel.Visibility = Visibility.Visible;
                IndependentScalePanel.Visibility = Visibility.Collapsed;
                ScaleChanged?.Invoke(ScaleSlider.Value, ScaleSlider.Value);
            }
        }

        public void SetInitialValues(double scaleX, double scaleY, double opacity, WpfColor tint, bool independent)
        {
            _independentScale = independent;
            _currentTint = tint;

            IndependentScaleCheckBox.IsChecked = independent;

            if (independent)
            {
                ScaleXSlider.Value = scaleX;
                ScaleYSlider.Value = scaleY;
                ScaleXValueText.Text = $"{scaleX:P0}";
                ScaleYValueText.Text = $"{scaleY:P0}";
                UnifiedScalePanel.Visibility = Visibility.Collapsed;
                IndependentScalePanel.Visibility = Visibility.Visible;
            }
            else
            {
                ScaleSlider.Value = scaleX;
                ScaleValueText.Text = $"{scaleX:P0}";
                UnifiedScalePanel.Visibility = Visibility.Visible;
                IndependentScalePanel.Visibility = Visibility.Collapsed;
            }

            OpacitySlider.Value = opacity;
            OpacityValueText.Text = $"{opacity:P0}";
            ColorPreview.Fill = new SolidColorBrush(tint);
        }
    }
}