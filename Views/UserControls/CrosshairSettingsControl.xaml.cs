using WpfColor = System.Windows.Media.Color;
using System.Windows.Media;

namespace plomfX.Views.UserControls
{
    public partial class CrosshairSettingsControl : System.Windows.Controls.UserControl
    {
        public event Action<double>? ScaleChanged;
        public event Action<double>? OpacityChanged;
        public event Action<WpfColor>? TintChanged;

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
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var wpfColor = WpfColor.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
                    ColorPreview.Fill = new SolidColorBrush(wpfColor);
                    TintChanged?.Invoke(wpfColor);
                }
            };
        }
        public void SetInitialValues(double scale, double opacity, WpfColor tint)
        {
            ScaleSlider.Value = scale;
            OpacitySlider.Value = opacity;
            ColorPreview.Fill = new SolidColorBrush(tint);
            ScaleValueText.Text = $"{scale:P0}";
            OpacityValueText.Text = $"{opacity:P0}";
        }
    }
}