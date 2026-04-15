using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Views
{
    public partial class OverlayWindow : Window
    {
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

        private static readonly Dictionary<(string path, WpfColor tint), BitmapSource> _tintedCache = new();

        private BitmapSource? _originalBitmap;
        private string? _currentImagePath;

        public OverlayWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        public void SetOriginalBitmap(BitmapSource bitmap, string imagePath)
        {
            _originalBitmap = bitmap;
            _currentImagePath = imagePath;
            CrosshairImage.Source = bitmap;
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
            if (_originalBitmap == null || string.IsNullOrEmpty(_currentImagePath)) return;

            var key = (_currentImagePath, tintColor);
            if (!_tintedCache.TryGetValue(key, out var tinted))
            {
                tinted = ApplyColorTint(_originalBitmap, tintColor);
                if (tinted != null)
                {
                    tinted.Freeze();
                    _tintedCache[key] = tinted;
                }
            }
            CrosshairImage.Source = tinted;
        }

        private BitmapSource ApplyColorTint(BitmapSource source, WpfColor tint)
        {
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
                pixels[i] = (byte)(pixels[i] * bFactor);
                pixels[i + 1] = (byte)(pixels[i + 1] * gFactor);
                pixels[i + 2] = (byte)(pixels[i + 2] * rFactor);
            }

            var result = new WriteableBitmap(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        public static void ClearTintCache()
        {
            _tintedCache.Clear();
        }

        // Keep SetCrosshairImage for backward compatibility if needed, but it's now obsolete
        public void SetCrosshairImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                SetOriginalBitmap(bitmap, imagePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load crosshair: {ex.Message}");
            }
        }
    }
}