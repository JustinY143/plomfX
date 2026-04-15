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
        private readonly Dictionary<string, List<string>> _categoryImages = new();
        private bool _categoriesLoaded = false;
        private static readonly Dictionary<string, BitmapImage> _thumbnailCache = new();

        public CrosshairBrowserControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            CategoryTabs.SelectionChanged += OnTabSelectionChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_categoriesLoaded)
            {
                LoadCategoryNames(); // Only create empty tabs, don't load images yet
                _categoriesLoaded = true;
            }
        }

        private void LoadCategoryNames()
        {
            var categories = CrosshairService.GetCategories();
            foreach (var cat in categories)
            {
                _categoryImages[cat.Name] = cat.ImagePaths;
                var tabItem = new TabItem { Header = cat.Name.ToUpper(), Tag = cat.Name };
                tabItem.Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new WrapPanel { Margin = new Thickness(5) }
                };
                CategoryTabs.Items.Add(tabItem);
            }
        }
        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear previously selected tab's panel to free memory
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem oldTab &&
                oldTab.Content is ScrollViewer oldSv && oldSv.Content is WrapPanel oldWp)
            {
                foreach (var child in oldWp.Children)
                {
                    if (child is System.Windows.Controls.Button btn && btn.Content is System.Windows.Controls.Image img)
                    {
                        img.Source = null;  // Release reference to BitmapImage
                        btn.Content = null;
                    }
                }
                oldWp.Children.Clear();
                GC.Collect(); 
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            // Load new tab's thumbnails if not already loaded
            if (CategoryTabs.SelectedItem is TabItem tab && tab.Content is ScrollViewer scrollViewer)
            {
                var wrapPanel = scrollViewer.Content as WrapPanel;
                if (wrapPanel != null && wrapPanel.Children.Count == 0)
                {
                    if (tab.Tag is string categoryName && _categoryImages.TryGetValue(categoryName, out var paths))
                    {
                        foreach (var path in paths)
                        {
                            var btn = CreateThumbnailButton(path);
                            wrapPanel.Children.Add(btn);
                        }
                    }
                }
            }
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
            var bitmap = new BitmapImage();
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.DecodePixelWidth = 64;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            _thumbnailCache[imagePath] = bitmap;

            var image = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Stretch = System.Windows.Media.Stretch.Uniform,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

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
                Content = image,
                Tag = imagePath
            };

            // Apply the global action button style for consistent hover effects
            var style = FindResource("ActionButtonStyle") as Style;
            if (style != null)
                button.Style = style;

            button.Click += (s, e) => CrosshairSelected?.Invoke(imagePath);
            return button;
        }
    }
}