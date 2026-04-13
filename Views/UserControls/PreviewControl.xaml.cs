using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views.UserControls
{
    public partial class PreviewControl : System.Windows.Controls.UserControl
    { 
        private BitmapSource? _originalPreviewBitmap;
        public PreviewControl()
        {
            InitializeComponent();
        }

        public void SetPreviewImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                _originalPreviewBitmap = bitmap;
                PreviewImage.Source = bitmap;
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }
        public void SetOpacity(double opacity)
        {
            PreviewImage.Opacity = opacity;
        }

        public void SetColorTint(WpfColor tint)
        {
            if (_originalPreviewBitmap == null) return;
            var tinted = ApplyColorTint(_originalPreviewBitmap, tint);
            PreviewImage.Source = tinted;
        }

        public void SetScale(double scale)
        {
            PreviewScaleTransform.ScaleX = scale;
            PreviewScaleTransform.ScaleY = scale;
        }

        private BitmapSource ApplyColorTint(BitmapSource source, WpfColor tint)
        {
            // Convert to Bgra32 if needed
            var formatConverted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = formatConverted.PixelWidth;
            int height = formatConverted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            formatConverted.CopyPixels(pixels, stride, 0);

            float rFactor = tint.R / 255f;
            float gFactor = tint.G / 255f;
            float bFactor = tint.B / 255f;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                // BGRA order
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                // byte a = pixels[i + 3];

                pixels[i] = (byte)(b * bFactor);
                pixels[i + 1] = (byte)(g * gFactor);
                pixels[i + 2] = (byte)(r * rFactor);
                // alpha unchanged
            }

            var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }
    }
}