using System.Windows;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views.UserControls
{
    public partial class CrosshairSettingsControl : System.Windows.Controls.UserControl
    {
        public event Action<double>? ScaleChanged;
        public event Action<double>? OpacityChanged;
        public event Action<WpfColor>? TintChanged;
        private WpfColor _currentTint = Colors.White;
        public event Action? BackRequested;

        public CrosshairSettingsControl()
        {
            InitializeComponent();
            
            ScaleSlider.ValueChanged += (s, e) =>
            {
                ScaleValueText.Text = $"{e.NewValue:P0}";
                ScaleChanged?.Invoke(e.NewValue);
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
        }
        public void SetInitialValues(double scale, double opacity, WpfColor tint)
        {
            _currentTint = tint;
            ScaleSlider.Value = scale;
            OpacitySlider.Value = opacity;
            ColorPreview.Fill = new SolidColorBrush(tint);
            ScaleValueText.Text = $"{scale:P0}";
            OpacityValueText.Text = $"{opacity:P0}";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke();
        }
    }
}