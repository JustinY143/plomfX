using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views
{
    public partial class OverlayWindow : Window
    {
        // Win32 API constants for extended window styles
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public OverlayWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            // Get current extended style
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Add WS_EX_LAYERED and WS_EX_TRANSPARENT to make the window click-through
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // Ensure window remains topmost after style change
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        /// <summary>
        /// Clears the crosshair (hides overlay content).
        /// </summary>
        public void ClearCrosshair()
        {
            CrosshairImage.Source = null;
        }

        public void SetScale(double scaleFactor)
        {
            CrosshairScaleTransform.ScaleX = scaleFactor;
            CrosshairScaleTransform.ScaleY = scaleFactor;
        }

        public void SetOpacity(double opacity)
        {
            CrosshairImage.Opacity = opacity;
        }

        public void SetColorTint(WpfColor tintColor)
        {
            if (_originalBitmap == null) return;
                var tinted = ApplyColorTint(_originalBitmap, tintColor);
                CrosshairImage.Source = tinted;
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

        // Overload SetCrosshairImage to store original bitmap
        private BitmapSource? _originalBitmap;

        public void SetCrosshairImage(string imagePath)
        {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    _originalBitmap = bitmap;
                    CrosshairImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load crosshair: {ex.Message}");
                }
        }
    }
}