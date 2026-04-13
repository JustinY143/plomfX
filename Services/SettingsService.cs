using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace plomfX.Services
{
    public class AppSettings
    {
        public string ThemeName { get; set; } = "Dark";
        public string DefaultCrosshairPath { get; set; } = string.Empty;
        public double DefaultScale { get; set; } = 1.0;
        public double DefaultOpacity { get; set; } = 1.0;
        public WpfColor DefaultTint { get; set; } = Colors.White;
        public int SelectedMonitorIndex { get; set; } = 0; // default to primary monitor
    }

    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new ColorJsonConverter() }
        };

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
    }
}