using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using plomfX.Services;

namespace plomfX.Views.UserControls
{
    public partial class CrosshairBrowserControl : System.Windows.Controls.UserControl
    {
        public event Action<string>? CrosshairSelected;

        public CrosshairBrowserControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
        }

        private void LoadCategories()
        {
            CategoryTabs.Items.Clear();
            var categories = CrosshairService.GetCategories();

            foreach (var category in categories)
            {
                var tabItem = new TabItem { Header = category.Name.ToUpper() };
                var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                var wrapPanel = new WrapPanel { Margin = new Thickness(5) };

                foreach (var imagePath in category.ImagePaths)
                {
                    var button = CreateThumbnailButton(imagePath);
                    wrapPanel.Children.Add(button);
                }

                scrollViewer.Content = wrapPanel;
                tabItem.Content = scrollViewer;
                CategoryTabs.Items.Add(tabItem);
            }
        }

        private System.Windows.Controls.Button CreateThumbnailButton(string imagePath)
        {
            var button = new System.Windows.Controls.Button
            {
                Width = 64,
                Height = 64,
                Margin = new Thickness(4),
                Padding = new Thickness(2),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = imagePath
            };

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.DecodePixelWidth = 64; // Load only small version
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); 

            var image = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality); // Optional

            button.Content = image;
            button.Click += (s, e) => CrosshairSelected?.Invoke(imagePath);

            return button;
        }
    }
}