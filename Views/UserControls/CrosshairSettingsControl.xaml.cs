using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using System.Windows.Input;

namespace plomfX.Views.UserControls
{
    public partial class CrosshairSettingsControl : System.Windows.Controls.UserControl
    {
        public event Action<double, double, bool>? ScaleChanged;
        public event Action<double>? OpacityChanged;
        public event Action<double>? OffsetYChanged;
        public event Action<WpfColor>? TintChanged;
        public event Action? BackRequested;

        private WpfColor _currentTint = Colors.White;
        private bool _independentScale = false;
        private bool _useSliderForOffset = true;
        private bool _suppressOffsetEvents = false;

        public CrosshairSettingsControl()
        {
            InitializeComponent();

            IndependentScaleCheckBox.Checked += IndependentScale_Changed;
            IndependentScaleCheckBox.Unchecked += IndependentScale_Changed;

            ScaleSlider.ValueChanged += (s, e) =>
            {
                ScaleValueText.Text = $"{e.NewValue:P0}";
                if (!_independentScale)
                    ScaleChanged?.Invoke(e.NewValue, e.NewValue, false);
            };

            ScaleXSlider.ValueChanged += (s, e) =>
            {
                ScaleXValueText.Text = $"{e.NewValue:P0}";
                if (_independentScale)
                    ScaleChanged?.Invoke(e.NewValue, ScaleYSlider.Value, true);
            };

            ScaleYSlider.ValueChanged += (s, e) =>
            {
                ScaleYValueText.Text = $"{e.NewValue:P0}";
                if (_independentScale)
                    ScaleChanged?.Invoke(ScaleXSlider.Value, e.NewValue, true);
            };

            OpacitySlider.ValueChanged += (s, e) =>
            {
                OpacityValueText.Text = $"{e.NewValue:P0}";
                OpacityChanged?.Invoke(e.NewValue);
            };

            // Slider mode
            OffsetYSlider.ValueChanged += (s, e) =>
            {
                if (_suppressOffsetEvents) return;
                _suppressOffsetEvents = true;
                OffsetYTextBox.Text = ((int)e.NewValue).ToString();
                UpdateOffsetLabel(e.NewValue);
                _suppressOffsetEvents = false;
                OffsetYChanged?.Invoke(e.NewValue);
            };

            // Textbox mode
            OffsetYTextBox.TextChanged += (s, e) =>
            {
                if (_suppressOffsetEvents) return;
                if (int.TryParse(OffsetYTextBox.Text, out int val))
                {
                    val = Math.Clamp(val, -150, 150);
                    _suppressOffsetEvents = true;
                    OffsetYSlider.Value = val;
                    _suppressOffsetEvents = false;
                    OffsetYChanged?.Invoke(val);
                }
            };

            OffsetYTextBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(OffsetYTextBox.Text, out int val))
                    val = Math.Clamp(val, -150, 150);
                else
                    val = 0;
                OffsetYTextBox.Text = val.ToString();
                OffsetYSlider.Value = val;
            };

            OffsetYTextBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Up || e.Key == Key.Down)
                {
                    if (int.TryParse(OffsetYTextBox.Text, out int val))
                    {
                        val += e.Key == Key.Up ? 1 : -1;
                        val = Math.Clamp(val, -150, 150);
                        OffsetYTextBox.Text = val.ToString();
                        // Move cursor to end so it doesn't jump
                        OffsetYTextBox.CaretIndex = OffsetYTextBox.Text.Length;
                        e.Handled = true;
                    }
                }
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

        private void UpdateOffsetLabel(double value)
        {
            int v = (int)value;
            OffsetYValueText.Text = v > 0 ? $"+{v} px" : $"{v} px";
        }

        private void IndependentScale_Changed(object sender, RoutedEventArgs e)
        {
            _independentScale = IndependentScaleCheckBox.IsChecked == true;

            if (_independentScale)
            {
                ScaleXSlider.Value = ScaleSlider.Value;
                ScaleYSlider.Value = ScaleSlider.Value;
                UnifiedScalePanel.Visibility = Visibility.Collapsed;
                IndependentScalePanel.Visibility = Visibility.Visible;
                ScaleChanged?.Invoke(ScaleXSlider.Value, ScaleYSlider.Value, true);
            }
            else
            {
                ScaleSlider.Value = ScaleXSlider.Value;
                UnifiedScalePanel.Visibility = Visibility.Visible;
                IndependentScalePanel.Visibility = Visibility.Collapsed;
                ScaleChanged?.Invoke(ScaleSlider.Value, ScaleSlider.Value, false);
            }
        }

        /// <summary>
        /// Toggle between slider and textbox input for the Y offset.
        /// </summary>
        public void SetOffsetInputMode(bool useSlider)
        {
            _useSliderForOffset = useSlider;
            OffsetSliderPanel.Visibility = useSlider ? Visibility.Visible : Visibility.Collapsed;
            OffsetTextPanel.Visibility = useSlider ? Visibility.Collapsed : Visibility.Visible;
        }

        public void SetInitialValues(double scaleX, double scaleY, double opacity, double offsetY, WpfColor tint, bool independent, bool useSliderForOffset)
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

            // Set offset (both inputs stay in sync)
            _suppressOffsetEvents = true;
            OffsetYSlider.Value = offsetY;
            OffsetYTextBox.Text = ((int)offsetY).ToString();
            UpdateOffsetLabel(offsetY);
            _suppressOffsetEvents = false;

            SetOffsetInputMode(useSliderForOffset);

            ColorPreview.Fill = new SolidColorBrush(tint);
        }
    }
}