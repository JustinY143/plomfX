using System.IO;
using System.Windows.Media;   // WPF Color
using WpfColor = System.Windows.Media.Color;


namespace plomfX.Services
{
    public class Theme
    {
        public string Name { get; set; } = string.Empty; // Initialize to avoid CS8618
        public WpfColor PrimaryColor { get; set; }
        public WpfColor SecondaryColor { get; set; }
    }

    public static class ThemeManager
    {
        private const string ThemesFileName = "themes.txt";

        public static List<Theme> LoadThemes()
        {
            var themes = new List<Theme>();
            string themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemesFileName);

            if (!File.Exists(themesPath))
            {
                // Create a default themes file if missing
                CreateDefaultThemesFile(themesPath);
            }

            foreach (var line in File.ReadAllLines(themesPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    continue;

                // Format: ThemeName: R,G,B : R,G,B
                var parts = line.Split(':');
                if (parts.Length != 3)
                    continue;

                string name = parts[0].Trim();
                string primaryRgb = parts[1].Trim();
                string secondaryRgb = parts[2].Trim();

                if (TryParseRgb(primaryRgb, out WpfColor primaryColor) &&
                    TryParseRgb(secondaryRgb, out WpfColor secondaryColor))
                {
                    themes.Add(new Theme
                    {
                        Name = name,
                        PrimaryColor = primaryColor,
                        SecondaryColor = secondaryColor
                    });
                }
            }

            return themes;
        }

        private static bool TryParseRgb(string rgb, out WpfColor color)
        {
            color = Colors.Transparent;   // Not WpfColors
            var components = rgb.Split(',');
            if (components.Length != 3) return false;
            if (byte.TryParse(components[0].Trim(), out byte r) &&
                byte.TryParse(components[1].Trim(), out byte g) &&
                byte.TryParse(components[2].Trim(), out byte b))
            {
                color = WpfColor.FromRgb(r, g, b);
                return true;
            }
            return false;
        }

        private static void CreateDefaultThemesFile(string path)
        {
            string defaultContent = 
                @"# plomfX Themes File
                # Format: ThemeName: PrimaryR,G,B : SecondaryR,G,B
                # Primary = Background color, Secondary = Buttons/Accents

                Dark: 30,30,30 : 62,62,66
                Light: 240,240,240 : 200,200,200
                Tako: 87, 70, 102 : 233, 151, 119
                Red: 45,25,25 : 204,0,0
                Green: 25,45,30 : 0,204,102
                Blue: 25,30,45 : 0,122,204
                ";
            File.WriteAllText(path, defaultContent);
        }

        /// <summary>
        /// Applies the selected theme by updating the application's ResourceDictionary.
        /// </summary>
        public static void ApplyTheme(Theme theme)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var primaryBrush = new SolidColorBrush(theme.PrimaryColor);
            var secondaryBrush = new SolidColorBrush(theme.SecondaryColor);
            var hoverBrush = new SolidColorBrush(LightenColor(theme.SecondaryColor, 0.25f));
            var borderBrush = new SolidColorBrush(DarkenColor(theme.PrimaryColor, 0.2f));
            var foregroundBrush = new SolidColorBrush(GetContrastColor(theme.SecondaryColor));

            // Freeze brushes for performance and reduced memory overhead
            primaryBrush.Freeze();
            secondaryBrush.Freeze();
            hoverBrush.Freeze();
            borderBrush.Freeze();
            foregroundBrush.Freeze();

            // Replace existing resources (do not Add, as that may keep old ones)
            app.Resources["PrimaryBackgroundBrush"] = primaryBrush;
            app.Resources["SecondaryAccentBrush"] = secondaryBrush;
            app.Resources["SecondaryHoverBrush"] = hoverBrush;
            app.Resources["PrimaryBorderBrush"] = borderBrush;
            app.Resources["SecondaryForegroundBrush"] = foregroundBrush;
        }

        // Helper: Lighten a color by a factor (0.0 - 1.0)
        private static WpfColor LightenColor(WpfColor color, float factor)
        {
            return WpfColor.FromRgb(
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }

        private static WpfColor DarkenColor(WpfColor color, float factor)
        {
            return WpfColor.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }

        // Helper: Return black or white depending on brightness
        private static WpfColor GetContrastColor(WpfColor color)
        {
            double brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255.0;
            return brightness > 0.5 ? Colors.Black : Colors.White;
        }
    }
}